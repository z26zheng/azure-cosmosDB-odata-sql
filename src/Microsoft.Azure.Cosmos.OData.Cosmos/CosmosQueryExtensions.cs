using System;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Azure.Cosmos.OData.Cosmos
{
    /// <summary>
    /// Extension methods bridging <see cref="TranslatedQuery"/> to the Azure Cosmos SDK.
    /// </summary>
    public static class CosmosQueryExtensions
    {
        /// <summary>
        /// Convert a <see cref="TranslatedQuery"/> to a Cosmos SDK <see cref="QueryDefinition"/>
        /// with all parameters bound.
        /// </summary>
        public static QueryDefinition ToQueryDefinition(this TranslatedQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            var qd = new QueryDefinition(query.Sql);
            foreach (var kv in query.Parameters)
            {
                qd = qd.WithParameter(kv.Key, kv.Value);
            }

            return qd;
        }

        /// <summary>
        /// Convert a <see cref="TranslatedQuery"/>'s companion count SQL to a <see cref="QueryDefinition"/>.
        /// Returns null if no count SQL was generated.
        /// </summary>
        public static QueryDefinition? ToCountQueryDefinition(this TranslatedQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (string.IsNullOrEmpty(query.CountSql)) return null;

            var qd = new QueryDefinition(query.CountSql);
            // Count query may share parameters with the main query
            foreach (var kv in query.Parameters)
            {
                qd = qd.WithParameter(kv.Key, kv.Value);
            }

            return qd;
        }

        /// <summary>
        /// Create a <see cref="FeedIterator{T}"/> from a <see cref="TranslatedQuery"/>.
        /// </summary>
        public static FeedIterator<T> GetODataQueryIterator<T>(
            this Container container,
            TranslatedQuery query,
            string? continuationToken = null,
            QueryRequestOptions? requestOptions = null)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (query == null) throw new ArgumentNullException(nameof(query));

            var qd = query.ToQueryDefinition();
            return container.GetItemQueryIterator<T>(qd, continuationToken, requestOptions);
        }
    }
}
