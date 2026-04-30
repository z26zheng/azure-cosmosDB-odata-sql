using System;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Bitmask selecting which OData clauses participate in translation.
    /// </summary>
    [Flags]
    public enum TranslationClauses
    {
        /// <summary>No clauses (engine returns an empty SQL string).</summary>
        None = 0,

        /// <summary>Translate <c>$select</c>.</summary>
        Select = 1 << 0,

        /// <summary>Translate <c>$filter</c>.</summary>
        Filter = 1 << 1,

        /// <summary>Translate <c>$orderby</c>.</summary>
        OrderBy = 1 << 2,

        /// <summary>Translate <c>$top</c> / <c>$skip</c> (pagination).</summary>
        Pagination = 1 << 3,

        /// <summary>Translate <c>$count</c>.</summary>
        Count = 1 << 4,

        /// <summary>Translate <c>$apply</c> (aggregate / groupby).</summary>
        Apply = 1 << 5,

        /// <summary>Every supported clause.</summary>
        All = Select | Filter | OrderBy | Pagination | Count | Apply,
    }

    /// <summary>
    /// Controls whether the rendered SQL inlines literals or substitutes named parameters
    /// (the Cosmos best practice).
    /// </summary>
    public enum ParameterizationMode
    {
        /// <summary>
        /// All literals are emitted as <c>@p0</c>, <c>@p1</c>, ... and returned in
        /// <see cref="TranslatedQuery.Parameters"/>. This is the v3 default and the only mode
        /// that protects against SQL injection on user-supplied <c>additionalWhereClause</c>.
        /// </summary>
        Parameters,

        /// <summary>
        /// All literals are inlined into the SQL string. Useful for debugging or for callers that
        /// cannot consume parameters; <b>do not use with untrusted input</b>.
        /// </summary>
        Inline,
    }

    /// <summary>
    /// Pagination strategy emitted for <c>$top</c> / <c>$skip</c>.
    /// </summary>
    public enum PaginationMode
    {
        /// <summary>
        /// Emit <c>OFFSET m LIMIT n</c>. This is the modern Cosmos best practice and is
        /// continuation-token friendly. Default in v3.
        /// </summary>
        OffsetLimit,

        /// <summary>
        /// Emit <c>SELECT TOP n ...</c>. Provided for back-compat with v1; ignores <c>$skip</c>.
        /// Note that Cosmos ignores its continuation token when TOP is present.
        /// </summary>
        Top,
    }

    /// <summary>
    /// Immutable options bundle that controls the translation pipeline.
    /// </summary>
    public sealed record TranslationOptions
    {
        /// <summary>Default configuration: all clauses, parameterized SQL, OFFSET/LIMIT pagination.</summary>
        public static TranslationOptions Default { get; } = new TranslationOptions();

        /// <summary>Which OData clauses to translate.</summary>
        public TranslationClauses Clauses { get; init; } = TranslationClauses.All;

        /// <summary>Inline literals or substitute parameters.</summary>
        public ParameterizationMode Parameterization { get; init; } = ParameterizationMode.Parameters;

        /// <summary>Strategy for <c>$top</c> / <c>$skip</c>.</summary>
        public PaginationMode Pagination { get; init; } = PaginationMode.OffsetLimit;

        /// <summary>
        /// Document alias used in the FROM clause and member access.  Defaults to <c>"c"</c>.
        /// </summary>
        public string DocumentAlias { get; init; } = "c";

        /// <summary>
        /// Name of the entity / container in the <c>FROM</c> clause.  Defaults to <c>"c"</c>
        /// (which makes the FROM clause read <c>FROM c</c>).
        /// </summary>
        public string FromName { get; init; } = "c";

        /// <summary>
        /// Optional raw SQL fragment to be ANDed into the WHERE clause.
        /// <para>
        /// Treated as a parameterless SQL literal — <b>callers must NOT inline user input here</b>.
        /// Use <see cref="AdditionalParameters"/> if your fragment uses parameters.
        /// </para>
        /// </summary>
        public string? AdditionalWhereClause { get; init; }

        /// <summary>
        /// Parameters referenced by <see cref="AdditionalWhereClause"/>.
        /// </summary>
        public System.Collections.Generic.IReadOnlyDictionary<string, object?>? AdditionalParameters { get; init; }
    }
}
