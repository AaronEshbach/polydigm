namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// Well-known keys for HTTP-specific metadata stored in IEndpointMetadata.Extensions.
    /// The Polydigm metadata model is protocol-agnostic; HTTP properties use these keys.
    /// </summary>
    public static class HttpExtensionKeys
    {
        /// <summary>The HTTP verb for an endpoint (e.g. "GET", "POST").</summary>
        public const string Method = "http.method";
    }
}
