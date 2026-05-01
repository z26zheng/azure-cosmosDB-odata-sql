using System;

namespace Microsoft.Azure.Cosmos.OData
{
    /// <summary>
    /// Error codes for <see cref="ODataTranslationException"/> to enable safe external error reporting
    /// without leaking internal schema details.
    /// </summary>
    public enum ODataTranslationErrorCode
    {
        /// <summary>Unknown or unclassified error.</summary>
        Unknown = 0,
        /// <summary>An OData feature is not supported by the translator.</summary>
        UnsupportedFeature = 1,
        /// <summary>An OData expression is structurally invalid.</summary>
        InvalidExpression = 2,
        /// <summary>A query complexity limit was exceeded (MaxFilterDepth, MaxTop, etc.).</summary>
        ComplexityLimitExceeded = 3,
        /// <summary>A security constraint was violated (denied field, filter required, etc.).</summary>
        SecurityViolation = 4,
        /// <summary>A $filter clause is required but was not provided.</summary>
        FilterRequired = 5,
        /// <summary>A field name is not allowed by the AllowedFields/DeniedFields policy.</summary>
        FieldNotAllowed = 6,
    }

    /// <summary>
    /// Base exception thrown when an OData query cannot be translated to Cosmos DB SQL.
    /// </summary>
    public class ODataTranslationException : Exception
    {
        /// <summary>The error code for safe external reporting.</summary>
        public ODataTranslationErrorCode ErrorCode { get; }

        /// <inheritdoc />
        public ODataTranslationException()
        {
            ErrorCode = ODataTranslationErrorCode.Unknown;
        }

        /// <inheritdoc />
        public ODataTranslationException(string message, ODataTranslationErrorCode errorCode = ODataTranslationErrorCode.Unknown)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        /// <inheritdoc />
        public ODataTranslationException(string message, Exception innerException, ODataTranslationErrorCode errorCode = ODataTranslationErrorCode.Unknown)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Thrown when a feature of OData is not supported by the Cosmos translator.
    /// </summary>
    public sealed class UnsupportedODataFeatureException : ODataTranslationException
    {
        /// <inheritdoc />
        public UnsupportedODataFeatureException(string message)
            : base(message, ODataTranslationErrorCode.UnsupportedFeature)
        {
        }

        /// <inheritdoc />
        public UnsupportedODataFeatureException(string message, Exception innerException)
            : base(message, innerException, ODataTranslationErrorCode.UnsupportedFeature)
        {
        }
    }

    /// <summary>
    /// Thrown when an OData expression is structurally invalid.
    /// </summary>
    public sealed class InvalidODataExpressionException : ODataTranslationException
    {
        /// <inheritdoc />
        public InvalidODataExpressionException(string message)
            : base(message, ODataTranslationErrorCode.InvalidExpression)
        {
        }

        /// <inheritdoc />
        public InvalidODataExpressionException(string message, Exception innerException)
            : base(message, innerException, ODataTranslationErrorCode.InvalidExpression)
        {
        }
    }
}
