using System.Security.Claims;
using Polydigm.Pipeline;

namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// Adapts an ASP.NET Core ClaimsPrincipal to the Polydigm IAuthenticationContext model.
    /// Roles are sourced from ClaimTypes.Role claims.
    /// All claims are available in the Claims dictionary keyed by claim type.
    /// Multiple claims of the same type are joined with ", ".
    /// </summary>
    public sealed class ClaimsPrincipalAuthenticationContext : IAuthenticationContext
    {
        public string? UserId { get; }
        public string? Username { get; }
        public bool IsAuthenticated { get; }
        public IReadOnlyList<string> Roles { get; }
        public IReadOnlyDictionary<string, object> Claims { get; }
        public string? AuthenticationScheme { get; }

        public ClaimsPrincipalAuthenticationContext(ClaimsPrincipal principal)
        {
            IsAuthenticated = principal.Identity?.IsAuthenticated ?? false;
            UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            Username = principal.FindFirstValue(ClaimTypes.Name);
            AuthenticationScheme = principal.Identity?.AuthenticationType;

            Roles = principal.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();

            Claims = principal.Claims
                .GroupBy(c => c.Type)
                .ToDictionary<IGrouping<string, Claim>, string, object>(
                    g => g.Key,
                    g => string.Join(", ", g.Select(c => c.Value)));
        }
    }
}
