using Microsoft.Azure.Cosmos.OData.Ast;
using Microsoft.OData.UriParser;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Translates an OData <see cref="QueryNode"/> AST into an intermediate
    /// <see cref="SqlExpression"/> tree before rendering to SQL text.
    /// </summary>
    /// <remarks>
    /// This interface is useful for:
    /// <list type="bullet">
    ///   <item><description>Custom AST manipulation before rendering.</description></item>
    ///   <item><description>Debugging / inspecting the intermediate representation.</description></item>
    ///   <item><description>Testing individual expression translations without full query assembly.</description></item>
    /// </list>
    /// </remarks>
    public interface IODataToSqlExpressionTranslator
    {
        /// <summary>
        /// Translate a single OData <see cref="QueryNode"/> into an <see cref="SqlExpression"/>.
        /// </summary>
        SqlExpression TranslateExpression(QueryNode node);
    }
}
