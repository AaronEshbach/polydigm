# Execution Pipeline Architecture

Polydigm's execution pipeline provides a **composable, platform-agnostic** framework for processing requests. The pipeline is built from discrete components that can be replaced, reordered, or customized for different deployment scenarios.

## Pipeline Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    REQUEST FLOW                             │
├─────────────────────────────────────────────────────────────┤
│  Transport Layer (HTTP, gRPC, AMQP)                         │
│                      ↓                                      │
│  Transport Adapter   (Wire Format → IServiceRequest)        │
│                      (body remains as Stream)               │
│                      ↓                                      │
│  1. Request Logging  (Log request metadata)                 │
│                      ↓                                      │
│  2. Telemetry        (Start trace, extract context)         │
│                      ↓                                      │
│  3. Routing          (Match endpoint by path/method)        │
│                      ↓                                      │
│  4. Authorization    (Check user access to endpoint)        │
│                      ↓                                      │
│  5. Deserialization  (Stream → DTO, now we know type)       │
│                      ↓                                      │
│  6. Validation       (DTO → Validated Model)                │
│                      ↓                                      │
│  7. Execution        (Invoke application logic)             │
│                      ↓                                      │
│  8. Serialization    (Result → Stream)                      │
│                      ↓                                      │
│  9. Response Logging (Log response metadata)                │
│                      ↓                                      │
│  10. Telemetry       (Complete trace, record metrics)       │
│                      ↓                                      │
│  Transport Adapter   (IServiceResponse → Wire Format)       │
│                      ↓                                      │
│  Transport Layer (Send response)                            │
└─────────────────────────────────────────────────────────────┘
```

## Core Abstractions

### IExecutionContext

The context flows through the entire pipeline, carrying request/response data:

```csharp
public interface IExecutionContext
{
    IServiceRequest Request { get; set; }          // Protocol-agnostic request
    IServiceResponse Response { get; set; }        // Protocol-agnostic response
    IEndpointMetadata? Endpoint { get; set; }      // Matched endpoint

    object? DeserializedInput { get; set; }        // DTO (unvalidated)
    object? ValidatedInput { get; set; }           // Validated model
    object? Result { get; set; }                   // Handler result

    bool HasError { get; set; }                    // Error flag
    Exception? Error { get; set; }                 // Exception details

    IDictionary<string, object> Properties { get; } // Shared state
    CancellationToken CancellationToken { get; }   // Cancellation
    IServiceProvider? Services { get; }            // DI container
}
```

### IServiceRequest

Protocol-agnostic request model (body remains as stream):

```csharp
public interface IServiceRequest
{
    string Path { get; }                           // "/pets/{petId}"
    string Method { get; }                         // "GET", "POST", etc.
    IReadOnlyDictionary<string, string> Headers { get; }
    IReadOnlyDictionary<string, string> QueryParameters { get; }
    IDictionary<string, string> RouteParameters { get; }  // Populated after routing
    Stream? Body { get; }                          // Undeserialized body stream
    string? ContentType { get; }                   // For deserializer
    string CorrelationId { get; }                  // For tracing
    IReadOnlyDictionary<string, object> Properties { get; }
}
```

### IServiceResponse

Protocol-agnostic response model (body as stream after serialization):

```csharp
public interface IServiceResponse
{
    int StatusCode { get; set; }                   // 200, 404, etc.
    IDictionary<string, string> Headers { get; }
    Stream? Body { get; set; }                     // Serialized body stream
    string? ContentType { get; set; }              // "application/json", etc.
    string CorrelationId { get; set; }             // Matches request
    IDictionary<string, object> Properties { get; }
}
```

### IPipelineComponent

Each pipeline stage implements this interface:

```csharp
public interface IPipelineComponent<TContext> where TContext : IExecutionContext
{
    Task InvokeAsync(TContext context, PipelineDelegate<TContext> next);
}
```

Components can:
- Inspect/modify the context
- Perform work before calling `next`
- Perform work after calling `next`
- Short-circuit (skip `next`) to halt the pipeline

### IPipelineBuilder

Composes components into a pipeline:

```csharp
public interface IPipelineBuilder<TContext>
{
    IPipelineBuilder<TContext> Use(Func<PipelineDelegate<TContext>, PipelineDelegate<TContext>> middleware);
    IPipelineBuilder<TContext> Use<TComponent>() where TComponent : IPipelineComponent<TContext>;
    IPipelineBuilder<TContext> Use(IPipelineComponent<TContext> component);
    PipelineDelegate<TContext> Build();
}
```

## Key Design Decisions

### Deserialization After Routing

**Why the body remains as a stream until after routing:**

1. **Type Discovery**: We don't know what type to deserialize into until we've matched the endpoint
2. **Efficiency**: No need to deserialize if routing fails (404)
3. **Flexibility**: Different endpoints can use different serialization formats
4. **Observability**: Can log request metadata without deserializing the body
5. **Memory**: Avoid allocating objects for invalid/unauthorized requests

**Flow:**
```
Transport → IServiceRequest (body: Stream)
         → Routing (identify endpoint)
         → Deserialization (Stream → DTO of correct type)
```

### Transport Adapters

Transport adapters convert between wire format and `IServiceRequest`/`IServiceResponse`:

```csharp
// HTTP Adapter
var serviceRequest = new ServiceRequest
{
    Path = httpContext.Request.Path,
    Method = httpContext.Request.Method,
    Headers = httpContext.Request.Headers.ToDictionary(),
    QueryParameters = httpContext.Request.Query.ToDictionary(),
    Body = httpContext.Request.Body,  // Stream, not deserialized yet
    ContentType = httpContext.Request.ContentType,
    CorrelationId = httpContext.TraceIdentifier
};

var context = new ExecutionContext
{
    Request = serviceRequest,
    Response = new ServiceResponse()
};

await pipeline(context);  // Execute pipeline

// Convert response back to HTTP
httpContext.Response.StatusCode = context.Response.StatusCode;
await context.Response.Body.CopyToAsync(httpContext.Response.Body);
```

## Pipeline Components

### 1. Deserialization (ISerializer)

Converts stream to DTOs **after routing** (when we know the target type):

```csharp
public interface ISerializer
{
    // Deserialize from context.Request.Body → DTO
    Task<object?> DeserializeAsync(Stream input, Type targetType, CancellationToken ct);

    // Serialize from context.Result → context.Response.Body
    Task SerializeAsync(Stream output, object? value, CancellationToken ct);

    string ContentType { get; } // e.g., "application/json"
}
```

**Pipeline integration:**
```csharp
// Deserialization component (after routing)
var targetType = DetermineInputType(context.Endpoint);
context.DeserializedInput = await serializer.DeserializeAsync(
    context.Request.Body,
    targetType,
    context.CancellationToken);
```

**Implementations:**
- `JsonSerializer` - System.Text.Json
- `ProtobufSerializer` - Protocol Buffers
- `MessagePackSerializer` - MessagePack
- `XmlSerializer` - XML

### 2. Request Logging (IRequestLogger)

Logs/audits incoming requests and outgoing responses:

```csharp
public interface IRequestLogger
{
    Task LogRequestAsync(IExecutionContext context);
    Task LogResponseAsync(IExecutionContext context);
    Task LogErrorAsync(IExecutionContext context, Exception exception);
}
```

**Implementations:**
- `ConsoleRequestLogger` - Logs to console
- `StructuredLogger` - Structured logging (Serilog, NLog)
- `DatabaseLogger` - Logs to database
- `AuditLogger` - Compliance/audit logging

### 3. Telemetry (ITelemetryProvider)

Distributed tracing and metrics:

```csharp
public interface ITelemetryProvider
{
    IActivityScope StartActivity(string name, IExecutionContext context);
    void RecordMetric(string name, double value, IDictionary<string, object>? tags);
    void IncrementCounter(string name, long increment, IDictionary<string, object>? tags);
    void RecordDuration(string name, TimeSpan duration, IDictionary<string, object>? tags);
    TraceContext? ExtractTraceContext(IExecutionContext context);
    void InjectTraceContext(IExecutionContext context, TraceContext traceContext);
}
```

**Implementations:**
- `OpenTelemetryProvider` - OpenTelemetry
- `ApplicationInsightsProvider` - Azure Application Insights
- `DatadogProvider` - Datadog APM
- `NoOpTelemetryProvider` - Disable telemetry

### 4. Routing (IEndpointRouter)

Matches requests to endpoints:

```csharp
public interface IEndpointRouter
{
    Task<IEndpointMetadata?> RouteAsync(IExecutionContext context);
}
```

**Implementations:**
- `HttpPathRouter` - Matches HTTP path + method
- `GrpcServiceRouter` - Matches gRPC service.method
- `AmqpRoutingKeyRouter` - Matches AMQP routing key
- `GraphQLOperationRouter` - Matches GraphQL operation

### 5. Authorization (IAuthorizationProvider)

Checks if the authenticated user is authorized to access the endpoint:

```csharp
public interface IAuthorizationProvider
{
    Task<AuthorizationResult> AuthorizeAsync(
        IExecutionContext context,
        IEndpointMetadata endpoint,
        CancellationToken cancellationToken);
}

public interface IAuthenticationContext
{
    string? UserId { get; }
    string? Username { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Roles { get; }
    IReadOnlyDictionary<string, object> Claims { get; }
    string? AuthenticationScheme { get; }  // "Bearer", "SASL", etc.
}

public class AuthorizationResult
{
    public bool IsAuthorized { get; init; }
    public string? FailureReason { get; init; }
    public int StatusCode { get; init; }  // 401, 403
}
```

**Why after routing:**
- Know which endpoint is being accessed
- Can apply endpoint-specific authorization rules
- Can short-circuit before deserialization if unauthorized

**Authorization Requirements:**
```csharp
// Built-in requirements
new AuthenticatedUserRequirement()  // Any authenticated user
new RoleRequirement("admin", "moderator")  // Specific roles
new ClaimRequirement("permission", "read:pets")  // Specific claims
new PolicyRequirement("OwnerOnly", async (user, context) => {
    // Custom logic
    var petId = context.Request.RouteParameters["petId"];
    return await IsPetOwner(user.UserId, petId);
})
```

**Implementations:**
- `OAuth2AuthorizationProvider` - OAuth 2.0 / JWT token validation
- `SaslAuthorizationProvider` - SASL for AMQP
- `CbsAuthorizationProvider` - Claims-Based Security for Service Bus
- `ApiKeyAuthorizationProvider` - API key validation
- `RoleBasedAuthorizationProvider` - Simple role-based access
- `PolicyBasedAuthorizationProvider` - Custom policy evaluation

### 6. Validation (IValidationProvider)

Converts DTOs to validated models:

```csharp
public interface IValidationProvider
{
    Task<ValidationResult> ValidateAsync(object? dto, Type targetType, CancellationToken ct);
}
```

**Uses Polydigm's validation pattern:**
- Calls `TryCreate` or `Create` methods on validated types
- Returns validation errors if constraints fail
- Produces validated models with guaranteed invariants

### 7. Execution (IEndpointExecutor)

Invokes application handlers:

```csharp
public interface IEndpointExecutor
{
    Task<object?> ExecuteAsync(
        IEndpointMetadata endpoint,
        object? validatedInput,
        IExecutionContext context);
}
```

**Application handlers:**
```csharp
public class GetPetHandler : IEndpointHandler<GetPetRequest, Pet>
{
    public async Task<Pet> HandleAsync(GetPetRequest input, IExecutionContext context)
    {
        // Application logic here
        return await _repository.GetPetAsync(input.PetId);
    }
}
```

## Building a Pipeline

### Example: HTTP REST API Pipeline

```csharp
var builder = new PipelineBuilder<ExecutionContext>();

builder
    // Note: Transport adapter already converted HTTP → IServiceRequest
    .Use<RequestLoggingComponent>()      // Log request metadata
    .Use<TelemetryComponent>()           // Start trace
    .Use<HttpRoutingComponent>()         // Match endpoint by path/method
    .Use<AuthorizationComponent>()       // Check user access (OAuth2)
    .Use<DeserializationComponent>()     // Stream → DTO (now we know type)
    .Use<ValidationComponent>()          // DTO → Validated model
    .Use<ExecutionComponent>()           // Invoke handler
    .Use<SerializationComponent>()       // Result → Stream
    .Use<ResponseLoggingComponent>()     // Log response
    .Use<TelemetryComponent>();          // Complete trace

var pipeline = builder.Build();

// In ASP.NET Core middleware:
public async Task InvokeAsync(HttpContext httpContext)
{
    // Convert HTTP → IServiceRequest
    var serviceRequest = ConvertToServiceRequest(httpContext);

    var context = new ExecutionContext
    {
        Request = serviceRequest,
        Response = new ServiceResponse(),
        CancellationToken = httpContext.RequestAborted,
        Services = httpContext.RequestServices
    };

    // Execute pipeline
    await pipeline(context);

    // Convert IServiceResponse → HTTP
    await WriteHttpResponse(httpContext, context.Response);
}
```

### Example: gRPC Service Pipeline

```csharp
var builder = new PipelineBuilder<ExecutionContext>();

builder
    .Use<GrpcTelemetryComponent>()           // gRPC tracing
    .Use<GrpcRoutingComponent>()             // Match service.method
    .Use<AuthorizationComponent>()           // Check user access
    .Use<ProtobufDeserializationComponent>() // Stream → DTO (Protobuf)
    .Use<ValidationComponent>()              // DTO → Validated
    .Use<ExecutionComponent>()               // Invoke handler
    .Use<ProtobufSerializationComponent>();  // Result → Stream (Protobuf)

var pipeline = builder.Build();

// In gRPC service:
public override async Task<GetPetResponse> GetPet(GetPetRequest request, ServerCallContext serverContext)
{
    // Convert gRPC → IServiceRequest
    var serviceRequest = new ServiceRequest
    {
        Path = "PetService.GetPetById",
        Method = "unary",
        Body = SerializeToStream(request),  // Protobuf to stream
        ContentType = "application/protobuf"
    };

    var context = new ExecutionContext { Request = serviceRequest };
    await pipeline(context);

    // Convert IServiceResponse → gRPC
    return DeserializeFromStream<GetPetResponse>(context.Response.Body);
}
```

### Example: AMQP Consumer Pipeline

```csharp
var builder = new PipelineBuilder<ExecutionContext>();

builder
    .Use<AmqpTelemetryComponent>()              // Message tracing
    .Use<AmqpRoutingComponent>()                // Match routing key
    .Use<AuthorizationComponent>()              // Check user access (SASL/CBS)
    .Use<MessagePackDeserializationComponent>() // Stream → DTO (MessagePack)
    .Use<ValidationComponent>()                 // DTO → Validated
    .Use<ExecutionComponent>()                  // Invoke handler
    .Use<MessagePackSerializationComponent>();  // Result → Stream (MessagePack)

var pipeline = builder.Build();

// In AMQP consumer:
consumer.Received += async (model, ea) =>
{
    // Convert AMQP → IServiceRequest
    var serviceRequest = new ServiceRequest
    {
        Path = ea.RoutingKey,
        Method = "request",
        Body = new MemoryStream(ea.Body.ToArray()),
        ContentType = "application/msgpack",
        CorrelationId = ea.BasicProperties.CorrelationId
    };

    var context = new ExecutionContext { Request = serviceRequest };
    await pipeline(context);

    // Publish response
    channel.BasicPublish(
        exchange: "",
        routingKey: ea.BasicProperties.ReplyTo,
        body: ReadAllBytes(context.Response.Body));
};
```

## Platform Adapters

The pipeline is platform-agnostic. Platform adapters connect it to specific hosts:

### ASP.NET Core Middleware Adapter

```csharp
public class PolydigmMiddleware
{
    private readonly PipelineDelegate<HttpExecutionContext> _pipeline;

    public PolydigmMiddleware(RequestDelegate next, PipelineDelegate<HttpExecutionContext> pipeline)
    {
        _pipeline = pipeline;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        // Convert HttpContext → IExecutionContext
        var context = new HttpExecutionContext(httpContext);

        // Execute pipeline
        await _pipeline(context);

        // Write response from context
        await WriteResponseAsync(httpContext, context);
    }
}
```

### OWIN Middleware Adapter

```csharp
public class PolydigmOwinMiddleware
{
    private readonly Func<IDictionary<string, object>, Task> _next;
    private readonly PipelineDelegate<OwinExecutionContext> _pipeline;

    public async Task Invoke(IDictionary<string, object> environment)
    {
        var context = new OwinExecutionContext(environment);
        await _pipeline(context);
        await WriteOwinResponseAsync(environment, context);
    }
}
```

### gRPC Service Adapter

```csharp
public class PolydigmGrpcService : PetService.PetServiceBase
{
    private readonly PipelineDelegate<GrpcExecutionContext> _pipeline;

    public override async Task<GetPetResponse> GetPet(GetPetRequest request, ServerCallContext context)
    {
        var execContext = new GrpcExecutionContext(request, context);
        await _pipeline(execContext);
        return (GetPetResponse)execContext.SerializedResult!;
    }
}
```

## Error Handling

Components can handle errors at any stage:

```csharp
public class ErrorHandlingComponent : IPipelineComponent<IExecutionContext>
{
    public async Task InvokeAsync(IExecutionContext context, PipelineDelegate<IExecutionContext> next)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.HasError = true;
            context.Error = ex;
            context.StatusCode = 400;
            context.Result = new { errors = ex.Errors };
        }
        catch (NotFoundException ex)
        {
            context.HasError = true;
            context.Error = ex;
            context.StatusCode = 404;
            context.Result = new { message = ex.Message };
        }
        catch (Exception ex)
        {
            context.HasError = true;
            context.Error = ex;
            context.StatusCode = 500;
            context.Result = new { message = "Internal server error" };
        }
    }
}
```

## Benefits

1. **Platform Agnostic**: Same pipeline works on ASP.NET Core, OWIN, gRPC, AMQP, custom hosts
2. **Composable**: Mix and match components as needed
3. **Replaceable**: Swap implementations (e.g., JSON → Protobuf)
4. **Testable**: Test components in isolation
5. **Observable**: Built-in logging, metrics, tracing
6. **Validated**: Guaranteed valid models in handlers
7. **Consistent**: Same behavior across protocols

## Next Steps

- Implement standard pipeline components
- Create platform adapters (ASP.NET Core, gRPC, AMQP)
- Build pipeline builder implementation
- Add component unit tests
- Create integration tests across platforms
