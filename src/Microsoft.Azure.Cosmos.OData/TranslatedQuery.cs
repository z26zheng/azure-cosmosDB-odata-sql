using System.Collections.Generic;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Result of translating an OData query into Cosmos DB SQL.
    /// </summary>
    public sealed class TranslatedQuery
    {
        /// <summary>The fully-rendered SQL text.</summary>
        public string Sql { get; }

        /// <summary>
        /// The parameter values referenced by the SQL string.  When
        /// <see cref="ParameterizationMode.Inline"/> is used the dictionary will be empty.
        /// Keys include the leading <c>@</c> (for example <c>"@p0"</c>) so they can be passed
        /// directly to <c>QueryDefinition.WithParameter</c>.
        /// </summary>
        public IReadOnlyDictionary<string, object?> Parameters { get; }

        /// <summary>
        /// Optional companion SQL that returns the total count of matching items
        /// (when <c>$count=true</c> is requested). May be <c>null</c>.
        /// </summary>
        public string? CountSql { get; }

        /// <summary>Initializes a new <see cref="TranslatedQuery"/>.</summary>
        public TranslatedQuery(string sql, IDictionary<string, object?> parameters, string? countSql = null)
        {
            Sql = sql ?? string.Empty;
            Parameters = parameters != null
                ? new Dictionary<string, object?>(parameters)
                : new Dictionary<string, object?>();
            CountSql = countSql;
        }

        /// <inheritdoc />
        public override string ToString() => Sql;

        /// <summary>
        /// Implicit conversion: many callers (and the Cosmos SDK) want the bare SQL.
        /// </summary>
        public static implicit operator string(TranslatedQuery q) => q?.Sql ?? string.Empty;
    }
}
