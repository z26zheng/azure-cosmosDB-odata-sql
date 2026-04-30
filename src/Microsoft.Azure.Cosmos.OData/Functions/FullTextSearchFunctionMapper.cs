using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Functions
{
    /// <summary>
    /// Maps virtual OData functions <c>fulltextcontains(field, term)</c> /
    /// <c>fulltextscore(field, term)</c> to Cosmos full-text search system functions.
    /// </summary>
    public sealed class FullTextSearchFunctionMapper : ISqlFunctionMapper
    {
        private static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "fulltextcontains", "fulltextcontainsall", "fulltextcontainsany", "fulltextscore",
        };

        /// <inheritdoc />
        public bool CanMap(string odataFunctionName) =>
            odataFunctionName != null && Names.Contains(odataFunctionName);

        /// <inheritdoc />
        public SqlExpression Map(string odataFunctionName, IReadOnlyList<SqlExpression> arguments)
        {
            switch (odataFunctionName.ToLowerInvariant())
            {
                case "fulltextcontains":    return new SqlFunctionCall("FullTextContains", arguments);
                case "fulltextcontainsall": return new SqlFunctionCall("FullTextContainsAll", arguments);
                case "fulltextcontainsany": return new SqlFunctionCall("FullTextContainsAny", arguments);
                case "fulltextscore":       return new SqlFunctionCall("FullTextScore", arguments);
                default:
                    throw new UnsupportedODataFeatureException($"Function '{odataFunctionName}' is not supported.");
            }
        }
    }
}
