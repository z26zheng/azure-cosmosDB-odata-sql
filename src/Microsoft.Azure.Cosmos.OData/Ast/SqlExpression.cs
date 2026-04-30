using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Azure.Cosmos.OData.Ast
{
    /// <summary>
    /// Root of the small SQL expression tree produced by the OData visitor and consumed by the renderer.
    /// Records are used for value semantics and easy unit-testing.
    /// </summary>
    public abstract record SqlExpression
    {
        /// <summary>Returns a human-readable SQL-like representation for debugging.</summary>
        public abstract override string ToString();
    }

    /// <summary>A literal value such as <c>5</c>, <c>'hello'</c>, <c>true</c>, <c>null</c>.</summary>
    public sealed record SqlLiteral(object? Value) : SqlExpression
    {
        /// <inheritdoc />
        public override string ToString() => Value switch
        {
            null => "null",
            string s => $"'{s}'",
            bool b => b ? "true" : "false",
            _ => Value.ToString() ?? "null",
        };
    }

    /// <summary>A reference to a member of the document, e.g. <c>c.foo.bar</c>.</summary>
    public sealed record SqlMember(string Path) : SqlExpression
    {
        /// <inheritdoc />
        public override string ToString() => Path;
    }

    /// <summary>A binary operation (<c>=</c>, <c>AND</c>, <c>+</c>, ...).</summary>
    public sealed record SqlBinary(SqlBinaryOperator Operator, SqlExpression Left, SqlExpression Right) : SqlExpression
    {
        /// <inheritdoc />
        public override string ToString() => $"({Left} {OperatorToString(Operator)} {Right})";

        private static string OperatorToString(SqlBinaryOperator op) => op switch
        {
            SqlBinaryOperator.Equal => "=",
            SqlBinaryOperator.NotEqual => "!=",
            SqlBinaryOperator.GreaterThan => ">",
            SqlBinaryOperator.GreaterThanOrEqual => ">=",
            SqlBinaryOperator.LessThan => "<",
            SqlBinaryOperator.LessThanOrEqual => "<=",
            SqlBinaryOperator.And => "AND",
            SqlBinaryOperator.Or => "OR",
            SqlBinaryOperator.Add => "+",
            SqlBinaryOperator.Subtract => "-",
            SqlBinaryOperator.Multiply => "*",
            SqlBinaryOperator.Divide => "/",
            SqlBinaryOperator.Modulo => "%",
            _ => op.ToString(),
        };
    }

    /// <summary>A unary operation (<c>NOT</c>, <c>-</c>).</summary>
    public sealed record SqlUnary(SqlUnaryOperator Operator, SqlExpression Operand) : SqlExpression
    {
        /// <inheritdoc />
        public override string ToString() => Operator == SqlUnaryOperator.Not
            ? $"NOT({Operand})"
            : $"-{Operand}";
    }

    /// <summary>A function invocation, e.g. <c>CONTAINS(c.name, 'foo')</c>.</summary>
    public sealed record SqlFunctionCall(string Name, IReadOnlyList<SqlExpression> Arguments) : SqlExpression
    {
        /// <inheritdoc />
        public override string ToString() => $"{Name}({string.Join(", ", Arguments.Select(a => a.ToString()))})";
    }

    /// <summary>An <c>EXISTS(SELECT VALUE ... )</c> sub-query (used for collection any/all).</summary>
    public sealed record SqlExists(string RangeVariable, SqlExpression Source, SqlExpression? Predicate) : SqlExpression
    {
        /// <inheritdoc />
        public override string ToString() => Predicate != null
            ? $"EXISTS(SELECT VALUE {RangeVariable} FROM {RangeVariable} IN {Source} WHERE {Predicate})"
            : $"EXISTS(SELECT VALUE {RangeVariable} FROM {RangeVariable} IN {Source})";
    }

    /// <summary>A raw, already-rendered SQL fragment.  Used sparingly for things like the additional WHERE clause.</summary>
    public sealed record SqlRaw(string Text) : SqlExpression
    {
        /// <inheritdoc />
        public override string ToString() => Text;
    }

    /// <summary>A null literal — distinct from <see cref="SqlLiteral"/> so the renderer can emit <c>null</c> verbatim.</summary>
    public sealed record SqlNull : SqlExpression
    {
        /// <summary>Singleton instance.</summary>
        public static SqlNull Instance { get; } = new SqlNull();

        /// <inheritdoc />
        public override string ToString() => "null";
    }

    /// <summary>Supported SQL binary operators.</summary>
    public enum SqlBinaryOperator
    {
        /// <summary>=</summary>
        Equal,
        /// <summary>!=</summary>
        NotEqual,
        /// <summary>&gt;</summary>
        GreaterThan,
        /// <summary>&gt;=</summary>
        GreaterThanOrEqual,
        /// <summary>&lt;</summary>
        LessThan,
        /// <summary>&lt;=</summary>
        LessThanOrEqual,
        /// <summary>AND</summary>
        And,
        /// <summary>OR</summary>
        Or,
        /// <summary>+</summary>
        Add,
        /// <summary>-</summary>
        Subtract,
        /// <summary>*</summary>
        Multiply,
        /// <summary>/</summary>
        Divide,
        /// <summary>%</summary>
        Modulo,
    }

    /// <summary>Supported SQL unary operators.</summary>
    public enum SqlUnaryOperator
    {
        /// <summary>NOT</summary>
        Not,
        /// <summary>- (negate)</summary>
        Negate,
    }
}
