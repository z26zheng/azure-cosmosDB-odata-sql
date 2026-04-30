using Microsoft.OData.UriParser;
using Microsoft.OData.UriParser.Aggregation;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Framework-agnostic bundle of OData clauses produced by either
    /// <c>System.Web.OData</c> (Web API 2) or <c>Microsoft.AspNetCore.OData</c>
    /// (ASP.NET Core).  Adapter packages do the conversion from their
    /// <c>ODataQueryOptions</c> type into this bundle, so the engine
    /// itself does not depend on either of those frameworks.
    /// </summary>
    public sealed record ODataQueryClauses
    {
        /// <summary>The parsed <c>$filter</c> clause, if any.</summary>
        public FilterClause? Filter { get; init; }

        /// <summary>The parsed <c>$orderby</c> clause, if any.</summary>
        public OrderByClause? OrderBy { get; init; }

        /// <summary>The parsed <c>$select</c> / <c>$expand</c> tree, if any.</summary>
        public SelectExpandClause? Select { get; init; }

        /// <summary>The parsed <c>$apply</c> clause, if any.</summary>
        public ApplyClause? Apply { get; init; }

        /// <summary>The numeric value of <c>$top</c>, if any.</summary>
        public long? Top { get; init; }

        /// <summary>The numeric value of <c>$skip</c>, if any.</summary>
        public long? Skip { get; init; }

        /// <summary>The boolean value of <c>$count</c>, if any.</summary>
        public bool? Count { get; init; }
    }
}
