# Pipeline Authorization

Authorization is a critical component in the Polydigm execution pipeline that determines whether an authenticated user is allowed to access a specific endpoint.

## Position in Pipeline

Authorization occurs **after routing but before deserialization**:

```
Transport Adapter → IServiceRequest
  ↓
Request Logging (metadata only)
  ↓
Telemetry (start trace)
  ↓
Routing (identify endpoint) ⭐
  ↓
Authorization (check user access) ⭐ NEW!
  ↓
Deserialization (only if authorized)
  ↓
Validation
  ↓
Execution
```

## Why After Routing?

1. **Know the endpoint**: Can apply endpoint-specific authorization rules
2. **Granular control**: Different endpoints have different access requirements
3. **Efficiency**: Skip deserialization if unauthorized (401/403 fast path)
4. **Security**: Validate access before processing untrusted input

## Core Abstractions

### IAuthorizationProvider

**[IAuthorizationProvider.cs](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IAuthorizationProvider.cs)** - Main authorization interface:

```csharp
public interface IAuthorizationProvider
{
    Task<AuthorizationResult> AuthorizeAsync(
        IExecutionContext context,
        IEndpointMetadata endpoint,
        CancellationToken cancellationToken);
}
```

### IAuthenticationContext

Represents the authenticated user:

```csharp
public interface IAuthenticationContext
{
    string? UserId { get; }           // "user-123", "alice@example.com"
    string? Username { get; }         // "alice"
    bool IsAuthenticated { get; }     // true/false
    IReadOnlyList<string> Roles { get; }  // ["admin", "user"]
    IReadOnlyDictionary<string, object> Claims { get; }  // Custom attributes
    string? AuthenticationScheme { get; }  // "Bearer", "SASL", "ApiKey"
}
```

**Set by transport adapter:**
```csharp
// HTTP: Extract from JWT token, session, etc.
var user = new AuthenticationContext
{
    UserId = jwt.Subject,
    Username = jwt.Name,
    IsAuthenticated = true,
    Roles = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value).ToList(),
    Claims = jwt.Claims.ToDictionary(c => c.Type, c => (object)c.Value),
    AuthenticationScheme = "Bearer"
};

context.User = user;
```

### AuthorizationResult

Result of authorization check:

```csharp
public class AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string? FailureReason { get; init; }
    public int StatusCode { get; init; }  // 401 or 403

    // Factory methods
    static AuthorizationResult Success();
    static AuthorizationResult Unauthorized(string? reason);  // 401
    static AuthorizationResult Forbidden(string? reason);     // 403
}
```

**401 vs 403:**
- **401 Unauthorized**: User is not authenticated (missing/invalid credentials)
- **403 Forbidden**: User is authenticated but not authorized (insufficient permissions)

## Authorization Requirements

### Built-in Requirements

**AuthenticatedUserRequirement** - Any authenticated user:
```csharp
new AuthenticatedUserRequirement()
// Returns true if user.IsAuthenticated == true
```

**RoleRequirement** - User must have one of the specified roles:
```csharp
new RoleRequirement("admin", "moderator")
// Returns true if user has "admin" OR "moderator" role
```

**ClaimRequirement** - User must have a specific claim:
```csharp
new ClaimRequirement("permission", "read:pets")
// Returns true if user has claim "permission" with value "read:pets"

new ClaimRequirement("tenant-id")  // Just check claim exists
// Returns true if user has any "tenant-id" claim
```

**PolicyRequirement** - Custom logic:
```csharp
new PolicyRequirement("OwnerOnly", async (user, context) =>
{
    var petId = context.Request.RouteParameters["petId"];
    var pet = await _repository.GetPetAsync(petId);
    return pet.OwnerId == user.UserId;
})
```

### Configuring Requirements

**Option 1: In endpoint metadata extensions**
```csharp
var endpoint = new EndpointMetadata
{
    Name = "DeletePet",
    Path = "/pets/{petId}",
    Extensions = new Dictionary<string, object>
    {
        ["authorization-requirements"] = new IAuthorizationRequirement[]
        {
            new AuthenticatedUserRequirement(),
            new RoleRequirement("admin")
        }
    }
};
```

**Option 2: In authorization provider configuration**
```csharp
var authProvider = new PolicyBasedAuthorizationProvider()
    .RequireAuthenticated("/pets/*")  // All pet endpoints
    .RequireRole("admin", "/admin/*")  // Admin endpoints
    .RequirePolicy("OwnerOnly", "/pets/{petId}", async (user, context) =>
    {
        // Custom logic
    });
```

**Option 3: Via attributes (when generating from OpenAPI)**
```yaml
/pets/{petId}:
  delete:
    security:
      - oauth2: [admin]  # Requires "admin" scope
```

## Protocol-Specific Implementations

### OAuth2 / JWT (REST APIs)

```csharp
public class OAuth2AuthorizationProvider : IAuthorizationProvider
{
    public async Task<AuthorizationResult> AuthorizeAsync(
        IExecutionContext context,
        IEndpointMetadata endpoint,
        CancellationToken cancellationToken)
    {
        var user = context.User;

        // Not authenticated
        if (!user?.IsAuthenticated == true)
            return AuthorizationResult.Unauthorized("Authentication required");

        // Extract required scopes from endpoint
        var requiredScopes = GetRequiredScopes(endpoint);
        if (requiredScopes.Count == 0)
            return AuthorizationResult.Success();

        // Check if user has required scopes
        if (!user.Claims.TryGetValue("scope", out var scopeClaim))
            return AuthorizationResult.Forbidden("Missing required scopes");

        var userScopes = scopeClaim.ToString()?.Split(' ') ?? Array.Empty<string>();
        var hasRequiredScope = requiredScopes.Any(rs => userScopes.Contains(rs));

        if (!hasRequiredScope)
            return AuthorizationResult.Forbidden($"Missing scope: {string.Join(", ", requiredScopes)}");

        return AuthorizationResult.Success();
    }
}
```

**Usage:**
```csharp
// In HTTP transport adapter
var token = httpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
var jwt = ValidateJwt(token);

context.User = new AuthenticationContext
{
    UserId = jwt.Subject,
    IsAuthenticated = true,
    Claims = jwt.Claims.ToDictionary(c => c.Type, c => (object)c.Value),
    AuthenticationScheme = "Bearer"
};
```

### SASL (AMQP)

```csharp
public class SaslAuthorizationProvider : IAuthorizationProvider
{
    public async Task<AuthorizationResult> AuthorizeAsync(
        IExecutionContext context,
        IEndpointMetadata endpoint,
        CancellationToken cancellationToken)
    {
        var user = context.User;

        if (!user?.IsAuthenticated == true)
            return AuthorizationResult.Unauthorized("SASL authentication required");

        // Check permissions based on AMQP routing key
        var routingKey = context.Request.Path;
        var requiredPermission = $"{routingKey}:send";

        if (!user.Claims.TryGetValue("permissions", out var permissions))
            return AuthorizationResult.Forbidden("No permissions granted");

        var userPermissions = permissions as IEnumerable<string> ?? Array.Empty<string>();
        if (!userPermissions.Contains(requiredPermission))
            return AuthorizationResult.Forbidden($"Missing permission: {requiredPermission}");

        return AuthorizationResult.Success();
    }
}
```

**Usage:**
```csharp
// In AMQP consumer
var username = basicDeliverEventArgs.BasicProperties.UserId;
var permissions = await _permissionStore.GetPermissionsAsync(username);

context.User = new AuthenticationContext
{
    UserId = username,
    Username = username,
    IsAuthenticated = true,
    Claims = new Dictionary<string, object>
    {
        ["permissions"] = permissions
    },
    AuthenticationScheme = "SASL"
};
```

### Claims-Based Security (Azure Service Bus)

```csharp
public class CbsAuthorizationProvider : IAuthorizationProvider
{
    public async Task<AuthorizationResult> AuthorizeAsync(
        IExecutionContext context,
        IEndpointMetadata endpoint,
        CancellationToken cancellationToken)
    {
        var user = context.User;

        if (!user?.IsAuthenticated == true)
            return AuthorizationResult.Unauthorized("CBS token required");

        // Verify token grants access to the specific queue/topic
        var resourceUri = context.Request.Properties["resource-uri"] as string;
        if (!user.Claims.TryGetValue("resource", out var grantedResource))
            return AuthorizationResult.Forbidden("Token does not grant resource access");

        if (!resourceUri.StartsWith(grantedResource.ToString()!))
            return AuthorizationResult.Forbidden($"Token not valid for resource: {resourceUri}");

        return AuthorizationResult.Success();
    }
}
```

### API Key

```csharp
public class ApiKeyAuthorizationProvider : IAuthorizationProvider
{
    private readonly IApiKeyStore _keyStore;

    public async Task<AuthorizationResult> AuthorizeAsync(
        IExecutionContext context,
        IEndpointMetadata endpoint,
        CancellationToken cancellationToken)
    {
        var user = context.User;

        if (!user?.IsAuthenticated == true)
            return AuthorizationResult.Unauthorized("API key required");

        // Check if API key has permission for this endpoint
        var apiKey = user.Claims["api-key"].ToString();
        var permissions = await _keyStore.GetPermissionsAsync(apiKey, cancellationToken);

        if (!permissions.Contains(endpoint.Path))
            return AuthorizationResult.Forbidden($"API key does not have access to {endpoint.Path}");

        return AuthorizationResult.Success();
    }
}
```

## Pipeline Integration

### Authorization Component

```csharp
public class AuthorizationComponent : IPipelineComponent<IExecutionContext>
{
    private readonly IAuthorizationProvider _authProvider;

    public async Task InvokeAsync(
        IExecutionContext context,
        PipelineDelegate<IExecutionContext> next)
    {
        // Skip if no endpoint (routing failed)
        if (context.Endpoint == null)
        {
            await next(context);
            return;
        }

        // Authorize
        var result = await _authProvider.AuthorizeAsync(
            context,
            context.Endpoint,
            context.CancellationToken);

        // If not authorized, short-circuit
        if (!result.IsAuthorized)
        {
            context.HasError = true;
            context.Response.StatusCode = result.StatusCode;
            context.Response.Body = SerializeError(new
            {
                error = result.StatusCode == 401 ? "unauthorized" : "forbidden",
                message = result.FailureReason
            });
            return;  // Don't call next - pipeline stops here
        }

        // Authorized - continue pipeline
        await next(context);
    }
}
```

### Example Pipeline

```csharp
var builder = new PipelineBuilder<ExecutionContext>();

builder
    .Use<RequestLoggingComponent>()
    .Use<TelemetryComponent>()
    .Use<RoutingComponent>()
    .Use<AuthorizationComponent>()        // ⭐ Authorization after routing
    .Use<DeserializationComponent>()      // Only if authorized
    .Use<ValidationComponent>()
    .Use<ExecutionComponent>()
    .Use<SerializationComponent>();

var pipeline = builder.Build();
```

## Benefits

### 1. Early Exit on Unauthorized Access

```
Old flow:
  Route → Deserialize (CPU/memory) → Validate → Authorize → 403

New flow:
  Route → Authorize → 403 (fast exit, no deserialization!)
```

### 2. Endpoint-Specific Authorization

```csharp
GET /pets → No auth required
GET /pets/{petId} → Authenticated user
DELETE /pets/{petId} → Admin role OR owner of pet
POST /admin/users → Admin role required
```

### 3. Protocol-Agnostic

Same authorization logic works for:
- HTTP REST (OAuth2/JWT)
- gRPC (mTLS, JWT metadata)
- AMQP (SASL, CBS)
- Custom protocols

### 4. Testable

```csharp
[Test]
public async Task AuthorizeAsync_AdminRole_Allowed()
{
    var user = new AuthenticationContext
    {
        IsAuthenticated = true,
        Roles = new[] { "admin" }
    };

    var context = new ExecutionContext { User = user };
    var endpoint = new EndpointMetadata { /* ... */ };

    var result = await _authProvider.AuthorizeAsync(context, endpoint, default);

    Assert.IsTrue(result.IsAuthorized);
}
```

## Next Steps

When implementing:

1. **Extract user from request** (in transport adapter)
2. **Set context.User** before pipeline
3. **Configure authorization provider** with requirements
4. **Add AuthorizationComponent** after routing
5. **Handle 401/403** appropriately for your protocol

The authorization system provides **flexible, protocol-agnostic access control** while maintaining **efficiency and security**! 🔒
