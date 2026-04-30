namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Maps OData property paths to Cosmos document member paths.
    /// </summary>
    /// <remarks>
    /// The default implementation simply prepends the document alias (<c>c.</c>).
    /// Custom implementations can adjust casing, route through a JSON path, etc.
    /// </remarks>
    public interface IFieldNameResolver
    {
        /// <summary>
        /// Translate a top-level field name (no dot in the input) into a SQL path.
        /// For example <c>"englishName"</c> -&gt; <c>"c.englishName"</c>.
        /// </summary>
        string TranslateFieldName(string fieldName);

        /// <summary>
        /// Translate a child access where the parent path has already been translated.
        /// For example (parent="c.address", child="city") -&gt; "c.address.city".
        /// </summary>
        string TranslateSource(string parentSqlPath, string childFieldName);

        /// <summary>
        /// Translate an enum literal — strips the namespace prefix that OData attaches to enum values.
        /// For example (<c>"NS.MyEnum'ONE'"</c>, <c>"NS.MyEnum"</c>) -&gt; <c>"'ONE'"</c>.
        /// </summary>
        string TranslateEnumValue(string literalText, string enumTypeName);
    }
}
