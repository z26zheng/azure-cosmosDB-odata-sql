using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos.OData.Ast;

namespace Microsoft.Azure.Cosmos.OData.Functions
{
    /// <summary>
    /// Maps a virtual <c>vectordistance(field, queryVector)</c> OData function to Cosmos
    /// <c>VectorDistance(...)</c>, enabling vector similarity search through OData.
    /// </summary>
    public sealed class VectorSearchFunctionMapper : ISqlFunctionMapper
    {
        /// <inheritdoc />
        public bool CanMap(string odataFunctionName) =>
            string.Equals(odataFunctionName, "vectordistance", StringComparison.OrdinalIgnoreCase);

        /// <inheritdoc />
        public SqlExpression Map(string odataFunctionName, IReadOnlyList<SqlExpression> arguments)
            => new SqlFunctionCall("VectorDistance", arguments);
    }
}
