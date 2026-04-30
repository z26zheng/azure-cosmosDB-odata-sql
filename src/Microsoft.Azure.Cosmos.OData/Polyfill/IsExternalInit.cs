// Polyfill so that record types (which use init-only setters) compile on netstandard2.0.
#if NETSTANDARD2_0 || NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    /// <summary>
    /// Reserved to be used by the compiler for tracking metadata. This class
    /// should not be used by developers in source code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
