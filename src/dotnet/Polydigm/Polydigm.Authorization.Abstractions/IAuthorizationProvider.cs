using Polydigm.Metadata;
using Polydigm.Pipeline;

namespace Polydigm.Authorization
{
    /// <summary>
    /// Handles authorization for endpoint access.
    /// Determines whether the authenticated user is allowed to execute a specific endpoint.
    /// Different implementations for OAuth2, SASL, CBS, custom policies, etc.
    /// </summary>
    public interface IAuthorizationProvider
    {
        /// <summary>
        /// Authorizes access to an endpoint for the current request.
        /// </summary>
        /// <param name="context">The execution context containing the request and user identity.</param>
        /// <param name="endpoint">The endpoint being accessed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Authorization result indicating success or failure.</returns>
        Task<AuthorizationResult> AuthorizeAsync(
            IExecutionContext context,
            IEndpointMetadata endpoint,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of an authorization check.
    /// </summary>
    public sealed class AuthorizationResult
    {
        /// <summary>
        /// Whether authorization succeeded.
        /// </summary>
        public bool IsAuthorized { get; init; }

        /// <summary>
        /// Human-readable reason for authorization failure.
        /// </summary>
        public string? FailureReason { get; init; }

        /// <summary>
        /// HTTP status code or equivalent for the failure.
        /// 401 Unauthorized (not authenticated), 403 Forbidden (authenticated but not authorized).
        /// </summary>
        public int StatusCode { get; init; }

        /// <summary>
        /// Additional metadata about the authorization decision.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }

        public static AuthorizationResult Success()
            => new() { IsAuthorized = true, StatusCode = 200 };

        public static AuthorizationResult Unauthorized(string? reason = null)
            => new()
            {
                IsAuthorized = false,
                StatusCode = 401,
                FailureReason = reason ?? "Authentication required"
            };

        public static AuthorizationResult Forbidden(string? reason = null)
            => new()
            {
                IsAuthorized = false,
                StatusCode = 403,
                FailureReason = reason ?? "Access denied"
            };
    }

    // NOTE: IAuthenticationContext and AuthenticationContext are defined in Polydigm.Pipeline.Abstractions

    /// <summary>
    /// Defines an authorization requirement that must be satisfied to access an endpoint.
    /// Examples: role requirement, permission requirement, custom policy.
    /// </summary>
    public interface IAuthorizationRequirement
    {
        /// <summary>
        /// Name of this requirement (e.g., "RequireRole:Admin", "RequirePermission:read:pets").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Checks if this requirement is satisfied for the given user and context.
        /// </summary>
        /// <param name="user">The authenticated user.</param>
        /// <param name="context">The execution context.</param>
        /// <returns>True if the requirement is satisfied.</returns>
        Task<bool> IsSatisfiedAsync(IAuthenticationContext user, IExecutionContext context);
    }

    /// <summary>
    /// Requires the user to be authenticated (any authenticated user).
    /// </summary>
    public sealed class AuthenticatedUserRequirement : IAuthorizationRequirement
    {
        public string Name => "AuthenticatedUser";

        public Task<bool> IsSatisfiedAsync(IAuthenticationContext user, IExecutionContext context)
        {
            return Task.FromResult(user.IsAuthenticated);
        }
    }

    /// <summary>
    /// Requires the user to have one of the specified roles.
    /// </summary>
    public sealed class RoleRequirement : IAuthorizationRequirement
    {
        public string Name { get; }
        public IReadOnlyList<string> AllowedRoles { get; init; }

        public RoleRequirement(params string[] roles)
        {
            AllowedRoles = roles;
            Name = $"RequireRole:{string.Join(",", roles)}";
        }

        public Task<bool> IsSatisfiedAsync(IAuthenticationContext user, IExecutionContext context)
        {
            if (!user.IsAuthenticated)
                return Task.FromResult(false);

            var hasRole = AllowedRoles.Any(role => user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase));
            return Task.FromResult(hasRole);
        }
    }

    /// <summary>
    /// Requires the user to have a specific claim with a specific value.
    /// </summary>
    public sealed class ClaimRequirement : IAuthorizationRequirement
    {
        public string Name { get; }
        public string ClaimType { get; init; }
        public object? ClaimValue { get; init; }

        public ClaimRequirement(string claimType, object? claimValue = null)
        {
            ClaimType = claimType;
            ClaimValue = claimValue;
            Name = claimValue != null
                ? $"RequireClaim:{claimType}={claimValue}"
                : $"RequireClaim:{claimType}";
        }

        public Task<bool> IsSatisfiedAsync(IAuthenticationContext user, IExecutionContext context)
        {
            if (!user.IsAuthenticated)
                return Task.FromResult(false);

            if (!user.Claims.TryGetValue(ClaimType, out var actualValue))
                return Task.FromResult(false);

            // If no specific value required, just check claim exists
            if (ClaimValue == null)
                return Task.FromResult(true);

            // Check if claim value matches
            var matches = ClaimValue.Equals(actualValue);
            return Task.FromResult(matches);
        }
    }

    /// <summary>
    /// Custom policy-based authorization requirement.
    /// </summary>
    public sealed class PolicyRequirement : IAuthorizationRequirement
    {
        public string Name { get; }
        public Func<IAuthenticationContext, IExecutionContext, Task<bool>> Policy { get; init; }

        public PolicyRequirement(string name, Func<IAuthenticationContext, IExecutionContext, Task<bool>> policy)
        {
            Name = name;
            Policy = policy;
        }

        public Task<bool> IsSatisfiedAsync(IAuthenticationContext user, IExecutionContext context)
        {
            return Policy(user, context);
        }
    }
}
