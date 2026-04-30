using System;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Base exception thrown when an OData query cannot be translated to Cosmos DB SQL.
    /// </summary>
    public class ODataTranslationException : Exception
    {
        /// <inheritdoc />
        public ODataTranslationException()
        {
        }

        /// <inheritdoc />
        public ODataTranslationException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public ODataTranslationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when a feature of OData is not supported by the Cosmos translator.
    /// </summary>
    public sealed class UnsupportedODataFeatureException : ODataTranslationException
    {
        /// <inheritdoc />
        public UnsupportedODataFeatureException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public UnsupportedODataFeatureException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Thrown when an OData expression is structurally invalid (e.g. a node kind we cannot interpret).
    /// </summary>
    public sealed class InvalidODataExpressionException : ODataTranslationException
    {
        /// <inheritdoc />
        public InvalidODataExpressionException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public InvalidODataExpressionException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
