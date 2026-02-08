namespace Polydigm.Pipeline
{
    /// <summary>
    /// Represents the authenticated user/principal making the request.
    /// Extracted from request headers, tokens, or session by the transport adapter.
    /// </summary>
    public interface IAuthenticationContext
    {
        /// <summary>
        /// Unique identifier for the user (e.g., "user-123", "alice@example.com").
        /// </summary>
        string? UserId { get; }

        /// <summary>
        /// Human-readable username.
        /// </summary>
        string? Username { get; }

        /// <summary>
        /// Whether the user is authenticated.
        /// False for anonymous requests.
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// User roles (e.g., "admin", "user", "moderator").
        /// </summary>
        IReadOnlyList<string> Roles { get; }

        /// <summary>
        /// User claims/attributes (e.g., tenant ID, permissions, custom attributes).
        /// </summary>
        IReadOnlyDictionary<string, object> Claims { get; }

        /// <summary>
        /// Authentication scheme used (e.g., "Bearer", "Basic", "ApiKey", "SASL").
        /// </summary>
        string? AuthenticationScheme { get; }
    }

    /// <summary>
    /// Default implementation of IAuthenticationContext.
    /// </summary>
    public sealed class AuthenticationContext : IAuthenticationContext
    {
        public string? UserId { get; init; }
        public string? Username { get; init; }
        public bool IsAuthenticated { get; init; }
        public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
        public IReadOnlyDictionary<string, object> Claims { get; init; } = new Dictionary<string, object>();
        public string? AuthenticationScheme { get; init; }

        /// <summary>
        /// Creates an anonymous (unauthenticated) context.
        /// </summary>
        public static AuthenticationContext Anonymous()
            => new() { IsAuthenticated = false };
    }
}
