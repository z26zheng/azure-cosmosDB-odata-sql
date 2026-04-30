using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Renders a <see cref="SqlExpression"/> tree to a SQL fragment, accumulating any parameters
    /// that were substituted along the way.
    /// </summary>
    public interface ISqlExpressionRenderer
    {
        /// <summary>Renders <paramref name="expression"/> and writes parameter substitutions into <paramref name="parameters"/>.</summary>
        string Render(SqlExpression expression, IDictionary<string, object?> parameters);
    }
}
