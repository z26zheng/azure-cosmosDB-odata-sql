using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Microsoft.OData.UriParser;

namespace Microsoft.Azure.Cosmos.OData.Tests.Helpers
{
    /// <summary>
    /// Lightweight helpers that build OData clauses from raw URIs without requiring ASP.NET.
    /// </summary>
    internal static class ODataTestHelper
    {
        /// <summary>Our mock EDM model.</summary>
        private static readonly Lazy<IEdmModel> Model = new Lazy<IEdmModel>(BuildModel);

        public static IEdmModel GetModel() => Model.Value;

        /// <summary>
        /// Parse a raw OData URI (e.g. <c>"http://localhost/Company?$filter=name eq 'x'"</c>)
        /// and return the parsed clauses.
        /// </summary>
        public static ODataQueryClauses Parse(string uri)
        {
            var fullUri = new Uri(uri);
            var parser = new ODataUriParser(GetModel(), new Uri("http://localhost/"), fullUri);
            parser.Resolver.EnableCaseInsensitive = true;

            return new ODataQueryClauses
            {
                Filter = parser.ParseFilter(),
                OrderBy = parser.ParseOrderBy(),
                Select = parser.ParseSelectAndExpand(),
                Apply = parser.ParseApply(),
                Top = parser.ParseTop(),
                Skip = parser.ParseSkip(),
                Count = parser.ParseCount(),
            };
        }

        /// <summary>
        /// Parse just the filter from a bare <c>$filter</c> value (no URI prefix).
        /// </summary>
        public static ODataQueryClauses FilterOnly(string filterExpression)
            => Parse("http://localhost/MockOpenType?$filter=" + Uri.EscapeDataString(filterExpression));

        /// <summary>
        /// Parse from a URI that may have $select, $filter, $top, $skip, $orderby, etc.
        /// </summary>
        public static ODataQueryClauses FromQuery(string query)
            => Parse("http://localhost/MockOpenType?" + query);

        private static IEdmModel BuildModel()
        {
            var builder = new ODataConventionModelBuilder();

            var entity = builder.EntitySet<MockOpenType>("MockOpenType").EntityType;
            entity.HasKey(t => t.Id);

            return builder.GetEdmModel();
        }
    }

    /// <summary>
    /// A mock open-typed entity used to test arbitrary property access in OData.
    /// </summary>
    public class MockOpenType
    {
        [Key]
        public string Id { get; set; } = default!;

        public string? EnglishName { get; set; }

        public MockEnum EnumNumber { get; set; }

        public int IntField { get; set; }

        public string? Property { get; set; }

        public string? CompanyId { get; set; }

        public string? P1 { get; set; }
        public string? P2 { get; set; }
        public string? P3 { get; set; }

        /// <summary>
        /// Open property bag — makes this entity an open type so untyped properties compile
        /// as OData dynamic property access (SingleValueOpenPropertyAccessNode).
        /// </summary>
        public IDictionary<string, object> DynamicProperties { get; set; } = new Dictionary<string, object>();
    }

    public enum MockEnum
    {
        ZERO = 0,
        ONE = 1,
        TWO = 2,
    }
}
