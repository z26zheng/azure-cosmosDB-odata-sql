using System;
using Microsoft.AspNetCore.OData.Query;

namespace Microsoft.Azure.Cosmos.OData.AspNetCore
{
    /// <summary>
    /// Extension methods that adapt ASP.NET Core's <see cref="ODataQueryOptions"/> /
    /// <see cref="ODataQueryOptions{T}"/> into the engine's <see cref="ODataQueryClauses"/>.
    /// </summary>
    public static class ODataQueryOptionsExtensions
    {
        /// <summary>
        /// Convert <see cref="ODataQueryOptions"/> to <see cref="ODataQueryClauses"/>.
        /// </summary>
        public static ODataQueryClauses ToQueryClauses(this ODataQueryOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            return new ODataQueryClauses
            {
                Filter = options.Filter?.FilterClause,
                OrderBy = options.OrderBy?.OrderByClause,
                Select = options.SelectExpand?.SelectExpandClause,
                Apply = options.Apply?.ApplyClause,
                Search = options.Search?.SearchClause,
                Top = options.Top?.Value,
                Skip = options.Skip?.Value,
                Count = options.Count?.Value,
            };
        }

        /// <summary>
        /// Translate <see cref="ODataQueryOptions"/> directly to Cosmos SQL.
        /// </summary>
        public static TranslatedQuery Translate(
            this ODataQueryOptions options,
            ODataToCosmosSqlTranslator translator,
            TranslationOptions? translationOptions = null)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (translator == null) throw new ArgumentNullException(nameof(translator));

            var clauses = options.ToQueryClauses();
            return translator.Translate(clauses, translationOptions ?? TranslationOptions.Default);
        }

        /// <summary>
        /// Translate <see cref="ODataQueryOptions{T}"/> directly to Cosmos SQL.
        /// </summary>
        public static TranslatedQuery Translate<T>(
            this ODataQueryOptions<T> options,
            ODataToCosmosSqlTranslator translator,
            TranslationOptions? translationOptions = null)
        {
            return Translate((ODataQueryOptions)options, translator, translationOptions);
        }
    }
}
