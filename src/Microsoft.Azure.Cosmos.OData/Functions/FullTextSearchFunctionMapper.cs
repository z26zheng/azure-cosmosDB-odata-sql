using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Functions
{
    /// <summary>
    /// Maps virtual OData functions for Cosmos full-text search to their SQL equivalents.
    /// </summary>
    /// <remarks>
    /// Supported functions:
    /// <list type="bullet">
    ///   <item><description><c>fulltextcontains(field, term)</c> → <c>FullTextContains(c.field, 'term')</c></description></item>
    ///   <item><description><c>fulltextcontainsall(field, term1, term2)</c> → <c>FullTextContainsAll(c.field, 'term1', 'term2')</c></description></item>
    ///   <item><description><c>fulltextcontainsany(field, term1, term2)</c> → <c>FullTextContainsAny(c.field, 'term1', 'term2')</c></description></item>
    ///   <item><description><c>fulltextscore(field, term)</c> → <c>FullTextScore(c.field, 'term')</c></description></item>
    ///   <item><description><c>rrf(score1, score2, ...)</c> → <c>RRF(score1, score2, ...)</c> (Reciprocal Rank Fusion for hybrid search)</description></item>
    /// </list>
    /// </remarks>
    public sealed class FullTextSearchFunctionMapper : ISqlFunctionMapper
    {
        private static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "fulltextcontains", "fulltextcontainsall", "fulltextcontainsany",
            "fulltextscore", "rrf",
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
                case "rrf":                 return new SqlFunctionCall("RRF", arguments);
                default:
                    throw new UnsupportedODataFeatureException($"Function '{odataFunctionName}' is not supported.");
            }
        }
    }
}
