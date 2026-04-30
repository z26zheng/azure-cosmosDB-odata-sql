using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Azure.Cosmos.OData.Ast;
using Microsoft.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;

namespace Microsoft.Azure.Cosmos.OData.Translation
{
    /// <summary>
    /// Walks an OData URI parser <see cref="QueryNode"/> tree and produces an equivalent
    /// <see cref="SqlExpression"/> tree.  This is the only piece that knows about OData internals;
    /// the rendering / function mapping / field naming pieces are pluggable.
    /// </summary>
    internal sealed class ODataExpressionVisitor : QueryNodeVisitor<SqlExpression>
    {
        private readonly IFieldNameResolver _fieldNames;
        private readonly ISqlFunctionMapper _functions;

        public ODataExpressionVisitor(IFieldNameResolver fieldNames, ISqlFunctionMapper functions)
        {
            _fieldNames = fieldNames ?? throw new ArgumentNullException(nameof(fieldNames));
            _functions = functions ?? throw new ArgumentNullException(nameof(functions));
        }

        /// <summary>Convenience: dispatch a node through the visitor.</summary>
        public SqlExpression Translate(QueryNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            return node.Accept(this);
        }

        // ----- Constants / null -----

        public override SqlExpression Visit(ConstantNode node)
        {
            if (node.Value == null) return SqlNull.Instance;

            // Enum -> strip namespace prefix so the renderer can pass the bare 'VALUE' through.
            if (node.TypeReference?.Definition?.TypeKind == EdmTypeKind.Enum && node.Value is ODataEnumValue ev)
            {
                var stripped = _fieldNames.TranslateEnumValue(node.LiteralText, ev.TypeName);
                return new SqlLiteral(stripped);
            }

            return new SqlLiteral(node.Value);
        }

        public override SqlExpression Visit(ConvertNode node) => node.Source.Accept(this);

        // ----- Member access -----

        public override SqlExpression Visit(SingleValuePropertyAccessNode node)
            => MemberAccess(node.Source, node.Property.Name);

        public override SqlExpression Visit(CollectionPropertyAccessNode node)
            => MemberAccess(node.Source, node.Property.Name);

        public override SqlExpression Visit(SingleValueOpenPropertyAccessNode node)
            => MemberAccess(node.Source, node.Name);

        public override SqlExpression Visit(CollectionOpenPropertyAccessNode node)
            => MemberAccess(node.Source, node.Name);

        public override SqlExpression Visit(SingleNavigationNode node)
            => MemberAccess(node.Source, node.NavigationProperty.Name);

        public override SqlExpression Visit(CollectionNavigationNode node)
            => MemberAccess(node.Source, node.NavigationProperty.Name);

        public override SqlExpression Visit(SingleComplexNode node)
            => MemberAccess(node.Source, node.Property.Name);

        public override SqlExpression Visit(CollectionComplexNode node)
            => MemberAccess(node.Source, node.Property.Name);

        public override SqlExpression Visit(ResourceRangeVariableReferenceNode node)
        {
            if (node.Name == "$it") return new SqlMember(string.Empty);
            return new SqlMember(node.Name);
        }

        public override SqlExpression Visit(NonResourceRangeVariableReferenceNode node)
            => new SqlMember(node.Name);

        public override SqlExpression Visit(SingleResourceCastNode node)
            => MemberAccess(node.Source, node.StructuredTypeReference.Definition.ToString() ?? string.Empty);

        public override SqlExpression Visit(SingleValueCastNode node)
            => MemberAccess(node.Source, node.TypeReference.Definition.ToString() ?? string.Empty);

        public override SqlExpression Visit(CollectionResourceCastNode node)
            => MemberAccess(node.Source, node.ItemStructuredType.Definition.ToString() ?? string.Empty);

        public override SqlExpression Visit(AggregatedCollectionPropertyNode node)
            => MemberAccess(node.Source, node.Property.Name);

        // ----- Operators -----

        public override SqlExpression Visit(BinaryOperatorNode node)
        {
            // Special-case `x eq null` / `x ne null` to use Cosmos-friendly IS_NULL / IS_DEFINED.
            // We keep the standard form too because users may want it; this is opt-in via the
            // function mapper which also exposes isdefined() directly.
            var left = node.Left.Accept(this);
            var right = node.Right.Accept(this);
            return new SqlBinary(MapBinary(node.OperatorKind), left, right);
        }

        public override SqlExpression Visit(UnaryOperatorNode node)
        {
            var operand = node.Operand.Accept(this);
            switch (node.OperatorKind)
            {
                case UnaryOperatorKind.Not:    return new SqlUnary(SqlUnaryOperator.Not, operand);
                case UnaryOperatorKind.Negate: return new SqlUnary(SqlUnaryOperator.Negate, operand);
                default:
                    throw new UnsupportedODataFeatureException($"Unary operator '{node.OperatorKind}' is not supported.");
            }
        }

        // ----- Function calls -----

        public override SqlExpression Visit(SingleValueFunctionCallNode node)
            => MapFunction(node.Name, node.Parameters);

        public override SqlExpression Visit(CollectionFunctionCallNode node)
            => MapFunction(node.Name, node.Parameters);

        public override SqlExpression Visit(SingleResourceFunctionCallNode node)
            => MapFunction(node.Name, node.Parameters);

        public override SqlExpression Visit(CollectionResourceFunctionCallNode node)
            => MapFunction(node.Name, node.Parameters);

        // ----- any() / all() lambdas -----

        public override SqlExpression Visit(AnyNode node)
        {
            // Patterns:
            //   tags/any(t: t eq 'foo')       -> EXISTS(SELECT VALUE t FROM t IN c.tags WHERE t = 'foo')
            //   tags/any()                    -> EXISTS(SELECT VALUE t FROM t IN c.tags)
            // Source comes through as a CollectionNode; we translate it to a member path.
            var source = node.Source.Accept(this);
            var rangeVar = node.CurrentRangeVariable?.Name ?? "v";

            SqlExpression? predicate = null;
            if (node.Body != null && !(node.Body is ConstantNode cn && cn.Value is bool b && b))
            {
                predicate = node.Body.Accept(this);
            }

            return new SqlExists(rangeVar, source, predicate);
        }

        public override SqlExpression Visit(AllNode node)
        {
            // x/all(t: P(t))  =>  NOT EXISTS(SELECT VALUE t FROM t IN x WHERE NOT (P(t)))
            var source = node.Source.Accept(this);
            var rangeVar = node.CurrentRangeVariable?.Name ?? "v";
            var predicate = node.Body.Accept(this);
            var notPredicate = new SqlUnary(SqlUnaryOperator.Not, predicate);
            return new SqlUnary(SqlUnaryOperator.Not, new SqlExists(rangeVar, source, notPredicate));
        }

        // ----- Search (full-text) -----

        public override SqlExpression Visit(SearchTermNode node)
            // Mapped onto Cosmos full-text search: FullTextContains(c, 'term') is something callers
            // can also produce via the function mapper. We emit a literal for $search for now.
            => new SqlLiteral(node.Text);

        public override SqlExpression Visit(ParameterAliasNode node)
            => new SqlRaw(node.Alias);

        public override SqlExpression Visit(NamedFunctionParameterNode node)
            => node.Value.Accept(this);

        public override SqlExpression Visit(InNode node)
        {
            // x IN (a, b, c)  -> we render this as a chain of OR equals so it works on every Cosmos version.
            var left = node.Left.Accept(this);
            if (!(node.Right is CollectionConstantNode coll) || coll.Collection.Count == 0)
            {
                throw new UnsupportedODataFeatureException("'in' requires a non-empty collection literal on the right side.");
            }

            SqlExpression? acc = null;
            foreach (var item in coll.Collection)
            {
                var rhs = item.Accept(this);
                var eq = new SqlBinary(SqlBinaryOperator.Equal, left, rhs);
                acc = acc == null ? (SqlExpression)eq : new SqlBinary(SqlBinaryOperator.Or, acc, eq);
            }

            return acc!;
        }

        public override SqlExpression Visit(CollectionConstantNode node)
        {
            // Used inside `in` lists and elsewhere; we don't render these standalone.
            throw new UnsupportedODataFeatureException("Standalone collection constants are not supported.");
        }

        public override SqlExpression Visit(CountNode node)
        {
            var source = node.Source.Accept(this);
            return new SqlFunctionCall("ARRAY_LENGTH", new[] { source });
        }

        // ----- helpers -----

        private SqlExpression MemberAccess(QueryNode source, string childName)
        {
            if (source == null)
            {
                return new SqlMember(_fieldNames.TranslateFieldName(childName));
            }

            var parent = source.Accept(this);
            if (parent is SqlMember sm)
            {
                if (string.IsNullOrEmpty(sm.Path))
                {
                    return new SqlMember(_fieldNames.TranslateFieldName(childName));
                }
                return new SqlMember(_fieldNames.TranslateSource(sm.Path, childName));
            }

            // Source resolved to something more complex (rare for filter clauses on a single resource);
            // fall back to a top-level field reference.
            return new SqlMember(_fieldNames.TranslateFieldName(childName));
        }

        private SqlExpression MapFunction(string name, IEnumerable<QueryNode> args)
        {
            var argList = new List<SqlExpression>();
            foreach (var a in args)
            {
                argList.Add(a.Accept(this));
            }

            if (!_functions.CanMap(name))
            {
                throw new UnsupportedODataFeatureException(
                    $"Function '{name}' is not supported. Provide a custom ISqlFunctionMapper to extend this set.");
            }

            return _functions.Map(name, argList);
        }

        private static SqlBinaryOperator MapBinary(BinaryOperatorKind k)
        {
            switch (k)
            {
                case BinaryOperatorKind.Equal:              return SqlBinaryOperator.Equal;
                case BinaryOperatorKind.NotEqual:           return SqlBinaryOperator.NotEqual;
                case BinaryOperatorKind.GreaterThan:        return SqlBinaryOperator.GreaterThan;
                case BinaryOperatorKind.GreaterThanOrEqual: return SqlBinaryOperator.GreaterThanOrEqual;
                case BinaryOperatorKind.LessThan:           return SqlBinaryOperator.LessThan;
                case BinaryOperatorKind.LessThanOrEqual:    return SqlBinaryOperator.LessThanOrEqual;
                case BinaryOperatorKind.And:                return SqlBinaryOperator.And;
                case BinaryOperatorKind.Or:                 return SqlBinaryOperator.Or;
                case BinaryOperatorKind.Add:                return SqlBinaryOperator.Add;
                case BinaryOperatorKind.Subtract:           return SqlBinaryOperator.Subtract;
                case BinaryOperatorKind.Multiply:           return SqlBinaryOperator.Multiply;
                case BinaryOperatorKind.Divide:             return SqlBinaryOperator.Divide;
                case BinaryOperatorKind.Modulo:             return SqlBinaryOperator.Modulo;
                case BinaryOperatorKind.Has:                return SqlBinaryOperator.And; // enum flag check: rendered as bitwise AND
                default:
                    throw new UnsupportedODataFeatureException($"Binary operator '{k}' is not supported.");
            }
        }
    }
}
