using System;

namespace Microsoft.Azure.Cosmos.OData.Naming
{
    /// <summary>
    /// Default <see cref="IFieldNameResolver"/> producing <c>c.field.subfield</c>-style paths.
    /// </summary>
    public sealed class DefaultFieldNameResolver : IFieldNameResolver
    {
        private readonly string _alias;
        private readonly string _aliasDot;

        /// <summary>Initializes a resolver using the given document alias (defaults to <c>"c"</c>).</summary>
        public DefaultFieldNameResolver(string documentAlias = "c")
        {
            if (string.IsNullOrWhiteSpace(documentAlias))
            {
                throw new ArgumentException("Document alias must be non-empty.", nameof(documentAlias));
            }

            _alias = documentAlias;
            _aliasDot = documentAlias + ".";
        }

        /// <inheritdoc />
        public string TranslateFieldName(string fieldName)
        {
            if (fieldName == null) throw new ArgumentNullException(nameof(fieldName));
            return _aliasDot + fieldName.Trim();
        }

        /// <inheritdoc />
        public string TranslateSource(string parentSqlPath, string childFieldName)
        {
            if (parentSqlPath == null) throw new ArgumentNullException(nameof(parentSqlPath));
            if (childFieldName == null) throw new ArgumentNullException(nameof(childFieldName));

            var combined = parentSqlPath.Trim() + "." + childFieldName.Trim();
            return combined.StartsWith(_aliasDot, StringComparison.Ordinal) || combined == _alias
                ? combined
                : _aliasDot + combined;
        }

        /// <inheritdoc />
        public string TranslateEnumValue(string literalText, string enumTypeName)
        {
            if (literalText == null) throw new ArgumentNullException(nameof(literalText));
            // OData renders enum literals as "NS.EnumType'VALUE'" — we strip the namespace prefix.
            // The result is "'VALUE'" which is then passed through unchanged to the renderer.
            if (!string.IsNullOrEmpty(enumTypeName) && literalText.Length > enumTypeName.Length &&
                literalText.StartsWith(enumTypeName, StringComparison.Ordinal))
            {
                return literalText.Substring(enumTypeName.Length).Trim();
            }

            return literalText;
        }
    }
}
