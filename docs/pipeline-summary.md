# Execution Pipeline - Implementation Summary

The Polydigm execution pipeline provides a **composable, platform-agnostic** framework for processing requests across different protocols (HTTP, gRPC, AMQP, etc.).

## What Was Created

### Core Pipeline Abstractions

**[IPipelineComponent](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IPipelineComponent.cs)**
```csharp
public interface IPipelineComponent<TContext> where TContext : IExecutionContext
{
    Task InvokeAsync(TContext context, PipelineDelegate<TContext> next);
}
```
- Represents a single stage in the pipeline
- Can inspect/modify context, perform work, or short-circuit
- Follows middleware pattern (similar to ASP.NET Core, OWIN, Express.js)

**[IPipelineBuilder](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IPipelineComponent.cs)**
```csharp
public interface IPipelineBuilder<TContext>
{
    IPipelineBuilder<TContext> Use(Func<PipelineDelegate<TContext>, PipelineDelegate<TContext>> middleware);
    IPipelineBuilder<TContext> Use<TComponent>() where TComponent : IPipelineComponent<TContext>;
    IPipelineBuilder<TContext> Use(IPipelineComponent<TContext> component);
    PipelineDelegate<TContext> Build();
}
```
- Composes components into an execution pipeline
- Fluent API for building pipelines

**[IExecutionContext](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IExecutionContext.cs)**
```csharp
public interface IExecutionContext
{
    IEndpointMetadata? Endpoint { get; set; }
    object? RawInput { get; set; }            // Transport layer
    object? DeserializedInput { get; set; }   // DTO
    object? ValidatedInput { get; set; }      // Validated model
    object? Result { get; set; }              // Handler result
    object? SerializedResult { get; set; }    // Wire format
    bool HasError { get; set; }
    Exception? Error { get; set; }
    int StatusCode { get; set; }
    IDictionary<string, object> Properties { get; }
    CancellationToken CancellationToken { get; }
    IServiceProvider? Services { get; }
}
```
- Flows through the entire pipeline
- Carries request/response data and metadata
- Thread-safe property bag for cross-component communication

### Component Interfaces

**[ISerializer](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/ISerializer.cs)**
- Deserializes wire format → DTOs
- Serializes results → wire format
- Supports Stream, byte[], and string
- ContentType property for negotiation

**Implementations:** JSON, Protobuf, MessagePack, XML

**[IValidationProvider](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IValidationProvider.cs)**
- Converts DTOs → Validated Models
- Uses Polydigm validation pattern (TryCreate/Create)
- Returns ValidationResult with errors or validated value

**[IRequestLogger](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IRequestLogger.cs)**
- Logs incoming requests
- Logs outgoing responses
- Logs errors/exceptions
- Audit trail for compliance

**Implementations:** Console, Structured (Serilog/NLog), Database, Audit

**[ITelemetryProvider](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/ITelemetryProvider.cs)**
- Distributed tracing (activities/spans)
- Metrics collection
- Trace context propagation
- OpenTelemetry compatible

**Implementations:** OpenTelemetry, Application Insights, Datadog, NoOp

**[IEndpointRouter](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IEndpointRouter.cs)**
- Matches requests to endpoints
- Extracts route parameters
- Returns endpoint metadata or 404

**Implementations:** HTTP path routing, gRPC service routing, AMQP routing, GraphQL operation routing

**[IEndpointExecutor](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IEndpointExecutor.cs)**
- Executes endpoint handlers
- Invokes application logic with validated input
- Returns handler result

**[IEndpointHandler\<TInput, TOutput\>](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IEndpointExecutor.cs)**
- Application handler interface
- Developers implement handlers for business logic
- Receives validated input, returns result

## Pipeline Flow

```
Request → Transport Adapter
            ↓
         [1. Deserialization]  Wire Format → DTO
            ↓
         [2. Request Logging]  Audit incoming request
            ↓
         [3. Telemetry]        Start distributed trace
            ↓
         [4. Routing]          Match endpoint by path
            ↓
         [5. Validation]       DTO → Validated Model
            ↓
         [6. Execution]        Invoke application handler
            ↓
         [7. Serialization]    Result → Wire Format
            ↓
         [8. Response Logging] Audit outgoing response
            ↓
         [9. Telemetry]        Complete trace, record metrics
            ↓
         Transport Adapter → Response
```

## Example Usage

### Building a Pipeline

```csharp
var builder = new PipelineBuilder<HttpExecutionContext>();

builder
    .Use<JsonDeserializationComponent>()
    .Use<RequestLoggingComponent>()
    .Use<OpenTelemetryComponent>()
    .Use<HttpRoutingComponent>()
    .Use<ValidationComponent>()
    .Use<ExecutionComponent>()
    .Use<JsonSerializationComponent>()
    .Use<ResponseLoggingComponent>();

var pipeline = builder.Build();

// Execute for each request
await pipeline(context);
```

### Implementing a Handler

```csharp
public class GetPetHandler : IEndpointHandler<GetPetRequest, Pet>
{
    private readonly IPetRepository _repository;

    public async Task<Pet> HandleAsync(GetPetRequest input, IExecutionContext context)
    {
        // Input is already validated - guaranteed valid PetId
        var pet = await _repository.GetAsync(input.PetId);

        if (pet == null)
            throw new NotFoundException($"Pet {input.PetId} not found");

        return pet;
    }
}
```

### Platform Adapters

The pipeline is platform-agnostic. Adapters connect it to specific hosts:

**ASP.NET Core:**
```csharp
app.UseMiddleware<PolydigmMiddleware>(pipeline);
```

**OWIN:**
```csharp
app.Use<PolydigmOwinMiddleware>(pipeline);
```

**gRPC:**
```csharp
public class PetServiceImpl : PetService.PetServiceBase
{
    private readonly PipelineDelegate<GrpcContext> _pipeline;

    public override async Task<GetPetResponse> GetPet(GetPetRequest request, ServerCallContext context)
    {
        var execContext = new GrpcExecutionContext(request, context);
        await _pipeline(execContext);
        return (GetPetResponse)execContext.SerializedResult!;
    }
}
```

**AMQP:**
```csharp
consumer.Received += async (model, ea) =>
{
    var context = new AmqpExecutionContext(ea.Body.ToArray(), ea.RoutingKey);
    await _pipeline(context);
    // Publish response or ACK
};
```

## Key Benefits

1. **Protocol Agnostic**: Same pipeline works on HTTP, gRPC, AMQP, custom protocols
2. **Composable**: Mix and match components as needed
3. **Replaceable**: Swap implementations (JSON → Protobuf, Console → Serilog)
4. **Testable**: Test components in isolation
5. **Observable**: Built-in logging, metrics, tracing hooks
6. **Type Safe**: Validated models guarantee invariants
7. **Consistent**: Same behavior across all protocols
8. **Extensible**: Add custom components for domain-specific logic

## Next Implementation Steps

1. **Create PipelineBuilder implementation**
2. **Implement standard components:**
   - JsonSerializer (System.Text.Json)
   - ValidationComponent (uses Polydigm validation)
   - ConsoleRequestLogger
   - NoOpTelemetryProvider
3. **Create platform adapters:**
   - ASP.NET Core middleware
   - gRPC service base
   - AMQP consumer
4. **Build integration tests**
5. **Add OpenTelemetry integration**
6. **Create developer examples**

## Documentation

- **[execution-pipeline.md](execution-pipeline.md)** - Complete pipeline architecture guide
- **[endpoint-metadata.md](endpoint-metadata.md)** - Endpoint metadata model
- **[endpoint-path-addressing.md](endpoint-path-addressing.md)** - Canonical endpoint addressing

## Project Structure

```
Polydigm.Execution.Abstractions/
├── IPipelineComponent.cs       - Pipeline component interface
├── IExecutionContext.cs        - Execution context
├── ISerializer.cs              - Serialization abstraction
├── IValidationProvider.cs      - Validation abstraction
├── IRequestLogger.cs           - Logging abstraction
├── ITelemetryProvider.cs       - Telemetry abstraction
├── IEndpointRouter.cs          - Routing abstraction
└── IEndpointExecutor.cs        - Execution abstraction
```

All interfaces are **platform-agnostic** and can be implemented for any execution environment!
