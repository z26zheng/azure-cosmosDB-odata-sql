using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.OData.Ast
{
    /// <summary>
    /// Root of the small SQL expression tree produced by the OData visitor and consumed by the renderer.
    /// Records are used for value semantics and easy unit-testing.
    /// </summary>
    public abstract record SqlExpression;

    /// <summary>A literal value such as <c>5</c>, <c>'hello'</c>, <c>true</c>, <c>null</c>.</summary>
    public sealed record SqlLiteral(object? Value) : SqlExpression;

    /// <summary>A reference to a member of the document, e.g. <c>c.foo.bar</c>.</summary>
    public sealed record SqlMember(string Path) : SqlExpression;

    /// <summary>A binary operation (<c>=</c>, <c>AND</c>, <c>+</c>, ...).</summary>
    public sealed record SqlBinary(SqlBinaryOperator Operator, SqlExpression Left, SqlExpression Right) : SqlExpression;

    /// <summary>A unary operation (<c>NOT</c>, <c>-</c>).</summary>
    public sealed record SqlUnary(SqlUnaryOperator Operator, SqlExpression Operand) : SqlExpression;

    /// <summary>A function invocation, e.g. <c>CONTAINS(c.name, 'foo')</c>.</summary>
    public sealed record SqlFunctionCall(string Name, IReadOnlyList<SqlExpression> Arguments) : SqlExpression;

    /// <summary>An <c>EXISTS(SELECT VALUE ... )</c> sub-query (used for collection any/all).</summary>
    public sealed record SqlExists(string RangeVariable, SqlExpression Source, SqlExpression? Predicate) : SqlExpression;

    /// <summary>A raw, already-rendered SQL fragment.  Used sparingly for things like the additional WHERE clause.</summary>
    public sealed record SqlRaw(string Text) : SqlExpression;

    /// <summary>A null literal — distinct from <see cref="SqlLiteral"/> so the renderer can emit <c>null</c> verbatim.</summary>
    public sealed record SqlNull : SqlExpression
    {
        /// <summary>Singleton instance.</summary>
        public static SqlNull Instance { get; } = new SqlNull();
    }

    /// <summary>Supported SQL binary operators.</summary>
    public enum SqlBinaryOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        And,
        Or,
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo,
    }

    /// <summary>Supported SQL unary operators.</summary>
    public enum SqlUnaryOperator
    {
        Not,
        Negate,
    }
}
