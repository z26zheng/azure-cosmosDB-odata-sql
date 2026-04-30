using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Functions
{
    /// <summary>
    /// Maps OData geospatial functions to Cosmos DB <c>ST_*</c> functions.
    /// </summary>
    public sealed class GeospatialFunctionMapper : ISqlFunctionMapper
    {
        private static readonly HashSet<string> Names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "geo.distance", "geo.intersects", "geo.length",
        };

        /// <inheritdoc />
        public bool CanMap(string odataFunctionName) =>
            odataFunctionName != null && Names.Contains(odataFunctionName);

        /// <inheritdoc />
        public SqlExpression Map(string odataFunctionName, IReadOnlyList<SqlExpression> arguments)
        {
            switch (odataFunctionName.ToLowerInvariant())
            {
                case "geo.distance":   return new SqlFunctionCall("ST_DISTANCE", arguments);
                case "geo.intersects": return new SqlFunctionCall("ST_INTERSECTS", arguments);
                case "geo.length":     return new SqlFunctionCall("ST_LENGTH", arguments);
                default:
                    throw new UnsupportedODataFeatureException($"Function '{odataFunctionName}' is not supported.");
            }
        }
    }
}
