using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Rendering
{
    /// <summary>
    /// Default renderer producing Cosmos DB SQL.  Honors <see cref="ParameterizationMode"/> by
    /// either inlining literals or substituting <c>@p0</c>, <c>@p1</c>, ... and writing the
    /// substitutions into the supplied parameter dictionary.
    /// </summary>
    public sealed class CosmosSqlRenderer : ISqlExpressionRenderer
    {
        private readonly ParameterizationMode _mode;
        private readonly string _parameterPrefix;

        /// <summary>Initializes a new renderer.</summary>
        public CosmosSqlRenderer(ParameterizationMode mode = ParameterizationMode.Parameters, string parameterPrefix = "p")
        {
            _mode = mode;
            _parameterPrefix = string.IsNullOrEmpty(parameterPrefix) ? "p" : parameterPrefix;
        }

        /// <inheritdoc />
        public string Render(SqlExpression expression, IDictionary<string, object?> parameters)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var sb = new StringBuilder();
            RenderInto(sb, expression, parameters);
            return sb.ToString();
        }

        private void RenderInto(StringBuilder sb, SqlExpression node, IDictionary<string, object?> parameters)
        {
            switch (node)
            {
                case SqlNull _:
                    sb.Append("null");
                    return;

                case SqlLiteral lit:
                    AppendLiteral(sb, lit.Value, parameters);
                    return;

                case SqlMember mem:
                    sb.Append(mem.Path);
                    return;

                case SqlRaw raw:
                    sb.Append(raw.Text);
                    return;

                case SqlUnary u:
                    RenderUnary(sb, u, parameters);
                    return;

                case SqlBinary b:
                    RenderBinary(sb, b, parameters);
                    return;

                case SqlFunctionCall f:
                    RenderFunction(sb, f, parameters);
                    return;

                case SqlExists e:
                    RenderExists(sb, e, parameters);
                    return;

                default:
                    throw new InvalidODataExpressionException($"Unknown SQL node {node.GetType().Name}.");
            }
        }

        private void RenderUnary(StringBuilder sb, SqlUnary u, IDictionary<string, object?> parameters)
        {
            switch (u.Operator)
            {
                case SqlUnaryOperator.Not:
                    sb.Append("NOT(");
                    RenderInto(sb, u.Operand, parameters);
                    sb.Append(')');
                    return;
                case SqlUnaryOperator.Negate:
                    sb.Append('-');
                    RenderInto(sb, u.Operand, parameters);
                    return;
                default:
                    throw new InvalidODataExpressionException($"Unknown unary operator {u.Operator}.");
            }
        }

        private void RenderBinary(StringBuilder sb, SqlBinary b, IDictionary<string, object?> parameters)
        {
            int prio = Precedence(b.Operator);

            // left
            if (b.Left is SqlBinary lb && Precedence(lb.Operator) < prio)
            {
                sb.Append('(');
                RenderInto(sb, lb, parameters);
                sb.Append(')');
            }
            else
            {
                RenderInto(sb, b.Left, parameters);
            }

            sb.Append(' ').Append(OperatorSymbol(b.Operator)).Append(' ');

            // right
            if (b.Right is SqlBinary rb && Precedence(rb.Operator) < prio)
            {
                sb.Append('(');
                RenderInto(sb, rb, parameters);
                sb.Append(')');
            }
            else
            {
                RenderInto(sb, b.Right, parameters);
            }
        }

        private void RenderFunction(StringBuilder sb, SqlFunctionCall f, IDictionary<string, object?> parameters)
        {
            sb.Append(f.Name).Append('(');
            for (int i = 0; i < f.Arguments.Count; i++)
            {
                if (i > 0) sb.Append(',');
                RenderInto(sb, f.Arguments[i], parameters);
            }
            sb.Append(')');
        }

        private void RenderExists(StringBuilder sb, SqlExists e, IDictionary<string, object?> parameters)
        {
            // EXISTS(SELECT VALUE <rangeVar> FROM <rangeVar> IN <source> WHERE <pred>)
            sb.Append("EXISTS(SELECT VALUE ").Append(e.RangeVariable)
              .Append(" FROM ").Append(e.RangeVariable).Append(" IN ");
            RenderInto(sb, e.Source, parameters);

            if (e.Predicate != null)
            {
                sb.Append(" WHERE ");
                RenderInto(sb, e.Predicate, parameters);
            }

            sb.Append(')');
        }

        private void AppendLiteral(StringBuilder sb, object? value, IDictionary<string, object?> parameters)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            // Strings that look like already-formatted SQL (start with ' or are an OData enum
            // literal that's already been stripped by the field name resolver) are passed through
            // verbatim only in Inline mode. In Parameters mode every literal becomes a parameter.
            if (_mode == ParameterizationMode.Inline)
            {
                sb.Append(InlineLiteral(value));
                return;
            }

            var paramName = "@" + _parameterPrefix + parameters.Count;
            parameters[paramName] = value;
            sb.Append(paramName);
        }

        private static string InlineLiteral(object value)
        {
            switch (value)
            {
                case string s:
                    // If this is already a quoted string literal (e.g. produced verbatim by OData LiteralText),
                    // pass it through; otherwise quote-escape it.
                    if (s.Length >= 2 && s[0] == '\'' && s[s.Length - 1] == '\'') return s;
                    return "'" + s.Replace("'", "''") + "'";
                case bool b:
                    return b ? "true" : "false";
                case DateTime dt:
                    return "'" + dt.ToString("o", CultureInfo.InvariantCulture) + "'";
                case DateTimeOffset dto:
                    return "'" + dto.ToString("o", CultureInfo.InvariantCulture) + "'";
                case Guid g:
                    return "'" + g.ToString("D") + "'";
                case IFormattable f:
                    return f.ToString(null, CultureInfo.InvariantCulture);
                default:
                    return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null";
            }
        }

        private static string OperatorSymbol(SqlBinaryOperator op)
        {
            switch (op)
            {
                case SqlBinaryOperator.Equal:              return "=";
                case SqlBinaryOperator.NotEqual:           return "!=";
                case SqlBinaryOperator.GreaterThan:        return ">";
                case SqlBinaryOperator.GreaterThanOrEqual: return ">=";
                case SqlBinaryOperator.LessThan:           return "<";
                case SqlBinaryOperator.LessThanOrEqual:    return "<=";
                case SqlBinaryOperator.And:                return "AND";
                case SqlBinaryOperator.Or:                 return "OR";
                case SqlBinaryOperator.Add:                return "+";
                case SqlBinaryOperator.Subtract:           return "-";
                case SqlBinaryOperator.Multiply:           return "*";
                case SqlBinaryOperator.Divide:             return "/";
                case SqlBinaryOperator.Modulo:             return "%";
                default:
                    throw new InvalidODataExpressionException($"Unknown binary operator {op}.");
            }
        }

        private static int Precedence(SqlBinaryOperator op)
        {
            switch (op)
            {
                case SqlBinaryOperator.Or: return 1;
                case SqlBinaryOperator.And: return 2;
                case SqlBinaryOperator.Equal:
                case SqlBinaryOperator.NotEqual:
                case SqlBinaryOperator.GreaterThan:
                case SqlBinaryOperator.GreaterThanOrEqual:
                case SqlBinaryOperator.LessThan:
                case SqlBinaryOperator.LessThanOrEqual: return 3;
                case SqlBinaryOperator.Add:
                case SqlBinaryOperator.Subtract: return 4;
                case SqlBinaryOperator.Multiply:
                case SqlBinaryOperator.Divide:
                case SqlBinaryOperator.Modulo: return 5;
                default: return -1;
            }
        }
    }
}
