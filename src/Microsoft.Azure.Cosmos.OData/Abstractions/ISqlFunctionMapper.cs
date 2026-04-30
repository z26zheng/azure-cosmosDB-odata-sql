using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Maps an OData built-in function call to a Cosmos SQL expression.
    /// </summary>
    /// <remarks>
    /// Splitting this responsibility away from the AST visitor lets callers register additional
    /// function families (geo, vector, full-text, custom UDFs) without modifying the engine.
    /// </remarks>
    public interface ISqlFunctionMapper
    {
        /// <summary>
        /// True if this mapper can translate the named OData function.
        /// </summary>
        bool CanMap(string odataFunctionName);

        /// <summary>
        /// Build the SQL expression for a recognized OData function call.
        /// </summary>
        /// <param name="odataFunctionName">Lowercase OData function name (e.g. <c>"contains"</c>).</param>
        /// <param name="arguments">The translated argument expressions.</param>
        SqlExpression Map(string odataFunctionName, IReadOnlyList<SqlExpression> arguments);
    }
}
