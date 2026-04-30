using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Functions
{
    /// <summary>
    /// Maps the OData v4 standard built-in functions to their Cosmos DB SQL equivalents.
    /// </summary>
    /// <remarks>
    /// Coverage:
    /// <list type="bullet">
    ///   <item><description>String: contains, startswith, endswith, length, indexof, substring, tolower, toupper, trim, concat, matchesPattern.</description></item>
    ///   <item><description>Type checks: isof / cast (limited).</description></item>
    ///   <item><description>Null checks: <c>x eq null</c> -&gt; <c>NOT IS_DEFINED(c.x) OR IS_NULL(c.x)</c> is handled in the visitor for the binary form; this mapper exposes <c>IS_DEFINED</c> via the OData function name <c>isdefined</c> as a Cosmos extension.</description></item>
    ///   <item><description>Math: round, floor, ceiling.</description></item>
    /// </list>
    /// Custom function families (geospatial, vector search, full-text search) live in dedicated mappers
    /// composed via <see cref="CompositeFunctionMapper"/>.
    /// </remarks>
    public sealed class DefaultFunctionMapper : ISqlFunctionMapper
    {
        private static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "contains", "startswith", "endswith", "length", "indexof", "substring",
            "tolower", "toupper", "trim", "concat", "matchespattern",
            "round", "floor", "ceiling",
            "year", "month", "day", "hour", "minute", "second",
            "isdefined", "arraycontains",
        };

        /// <inheritdoc />
        public bool CanMap(string odataFunctionName)
        {
            if (odataFunctionName == null) return false;
            return Names.Contains(odataFunctionName);
        }

        /// <inheritdoc />
        public SqlExpression Map(string odataFunctionName, IReadOnlyList<SqlExpression> arguments)
        {
            if (odataFunctionName == null) throw new ArgumentNullException(nameof(odataFunctionName));
            if (arguments == null) throw new ArgumentNullException(nameof(arguments));

            switch (odataFunctionName.ToLowerInvariant())
            {
                // String functions
                case "contains":      return new SqlFunctionCall("CONTAINS", arguments);
                case "startswith":    return new SqlFunctionCall("STARTSWITH", arguments);
                case "endswith":      return new SqlFunctionCall("ENDSWITH", arguments);
                case "length":        return new SqlFunctionCall("LENGTH", arguments);
                case "indexof":       return new SqlFunctionCall("INDEX_OF", arguments);
                case "substring":     return new SqlFunctionCall("SUBSTRING", arguments);
                case "tolower":       return new SqlFunctionCall("LOWER", arguments);
                case "toupper":       return new SqlFunctionCall("UPPER", arguments);
                case "concat":        return new SqlFunctionCall("CONCAT", arguments);
                case "matchespattern":return new SqlFunctionCall("RegexMatch", arguments);

                // trim() => LTRIM(RTRIM(x))
                case "trim":
                    return new SqlFunctionCall("LTRIM", new SqlExpression[]
                    {
                        new SqlFunctionCall("RTRIM", arguments),
                    });

                // Math
                case "round":         return new SqlFunctionCall("ROUND", arguments);
                case "floor":         return new SqlFunctionCall("FLOOR", arguments);
                case "ceiling":       return new SqlFunctionCall("CEILING", arguments);

                // Date/time
                case "year":          return new SqlFunctionCall("DateTimePart", PrependLiteral("yyyy", arguments));
                case "month":         return new SqlFunctionCall("DateTimePart", PrependLiteral("mm", arguments));
                case "day":           return new SqlFunctionCall("DateTimePart", PrependLiteral("dd", arguments));
                case "hour":          return new SqlFunctionCall("DateTimePart", PrependLiteral("hh", arguments));
                case "minute":        return new SqlFunctionCall("DateTimePart", PrependLiteral("mi", arguments));
                case "second":        return new SqlFunctionCall("DateTimePart", PrependLiteral("ss", arguments));

                // Cosmos extensions exposed as OData functions
                case "isdefined":     return new SqlFunctionCall("IS_DEFINED", arguments);
                case "arraycontains": return new SqlFunctionCall("ARRAY_CONTAINS", arguments);

                default:
                    throw new UnsupportedODataFeatureException($"Function '{odataFunctionName}' is not supported.");
            }
        }

        private static IReadOnlyList<SqlExpression> PrependLiteral(string literal, IReadOnlyList<SqlExpression> tail)
        {
            var list = new List<SqlExpression>(tail.Count + 1) { new SqlLiteral(literal) };
            for (int i = 0; i < tail.Count; i++) list.Add(tail[i]);
            return list;
        }
    }
}
