#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Polyfill for init-only properties in netstandard2.0.
    /// This type is required for C# 9 init accessors to work in older target frameworks.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}

#endif
