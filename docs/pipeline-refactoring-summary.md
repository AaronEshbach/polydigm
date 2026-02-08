# Pipeline Flow Refactoring Summary

## Key Change: Deserialization After Routing

The pipeline has been refactored to **defer deserialization until after routing**. This is a critical architectural improvement with several important benefits.

## Old Flow ❌

```
Transport → Deserialize (wire → DTO) → Route → Validate → Execute → Serialize
            ↑
            Problem: Don't know what type to deserialize into yet!
```

## New Flow ✅

```
Transport → IServiceRequest (body: Stream)
         → Route (identify endpoint)
         → Deserialize (Stream → DTO of correct type)
         → Validate (DTO → Validated model)
         → Execute
         → Serialize (Result → Stream)
         → IServiceResponse (body: Stream)
         → Transport
```

## What Changed

### 1. New Abstraction: IServiceRequest

**[IServiceRequest](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IServiceRequest.cs)** - Protocol-agnostic request model:

```csharp
public interface IServiceRequest
{
    string Path { get; }                    // "/pets/{petId}"
    string Method { get; }                  // "GET", "POST", etc.
    IReadOnlyDictionary<string, string> Headers { get; }
    IReadOnlyDictionary<string, string> QueryParameters { get; }
    IDictionary<string, string> RouteParameters { get; }  // Populated after routing
    Stream? Body { get; }                   // ⭐ Stays as stream!
    string? ContentType { get; }
    string CorrelationId { get; }
    IReadOnlyDictionary<string, object> Properties { get; }
}
```

**Key point:** `Body` is a `Stream`, not deserialized yet!

### 2. New Abstraction: IServiceResponse

**[IServiceResponse](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IServiceResponse.cs)** - Protocol-agnostic response model:

```csharp
public interface IServiceResponse
{
    int StatusCode { get; set; }
    IDictionary<string, string> Headers { get; }
    Stream? Body { get; set; }              // ⭐ Serialized as stream!
    string? ContentType { get; set; }
    string CorrelationId { get; set; }
    IDictionary<string, object> Properties { get; }
}
```

### 3. Updated: IExecutionContext

**[IExecutionContext](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IExecutionContext.cs)** now uses the new models:

```csharp
public interface IExecutionContext
{
    IServiceRequest Request { get; set; }   // Was: RawInput
    IServiceResponse Response { get; set; } // Was: SerializedResult

    IEndpointMetadata? Endpoint { get; set; }
    object? DeserializedInput { get; set; }  // Set AFTER routing
    object? ValidatedInput { get; set; }
    object? Result { get; set; }

    bool HasError { get; set; }
    Exception? Error { get; set; }
    // ... properties, cancellation, services
}
```

**Removed:**
- `RawInput` → Replaced by `Request` (with `Body` stream)
- `SerializedResult` → Replaced by `Response` (with `Body` stream)
- `StatusCode` → Moved to `Response.StatusCode`

## Why This Matters

### 1. Type Discovery

**Problem:** Can't deserialize without knowing the target type.

```csharp
// Old way - impossible!
var dto = await Deserialize(stream, ???);  // What type?

// New way - route first, then deserialize
await Route(context);  // context.Endpoint is now set
var inputType = GetInputType(context.Endpoint);
var dto = await Deserialize(context.Request.Body, inputType);
```

### 2. Efficiency

**Don't deserialize if routing fails:**

```csharp
// Old way
Deserialize (expensive!)
  ↓
Route → 404 Not Found (wasted work!)

// New way
Route → 404 Not Found (fast!)
  ↓
Never deserialize (saved work!)
```

### 3. Flexibility

**Different endpoints, different formats:**

```csharp
// Endpoint A: JSON
POST /pets (Content-Type: application/json)
  → Route to CreatePet endpoint
  → Deserialize JSON → Pet DTO

// Endpoint B: Protobuf
POST /pets (Content-Type: application/protobuf)
  → Route to CreatePet endpoint
  → Deserialize Protobuf → Pet DTO
```

### 4. Observability

**Log metadata without deserializing body:**

```csharp
// Request logging component
await LogAsync(new RequestLog
{
    Path = context.Request.Path,
    Method = context.Request.Method,
    Headers = context.Request.Headers,
    // Body not deserialized yet - save memory/CPU
});
```

### 5. Security

**Validate before deserializing:**

```csharp
// Route first
await Route(context);

// Check auth BEFORE deserializing
if (!IsAuthorized(context))
{
    context.Response.StatusCode = 401;
    return;  // Never deserialized untrusted input!
}

// Now safe to deserialize
await Deserialize(context);
```

## Updated Pipeline Flow

### Transport Adapter Responsibilities

**HTTP Adapter:**
```csharp
// Convert HttpContext → IServiceRequest
var serviceRequest = new ServiceRequest
{
    Path = httpContext.Request.Path,
    Method = httpContext.Request.Method,
    Headers = httpContext.Request.Headers.ToDictionary(),
    QueryParameters = httpContext.Request.Query.ToDictionary(),
    Body = httpContext.Request.Body,  // ⭐ Stream, not deserialized
    ContentType = httpContext.Request.ContentType,
    CorrelationId = httpContext.TraceIdentifier
};

var context = new ExecutionContext
{
    Request = serviceRequest,
    Response = new ServiceResponse()
};

// Execute pipeline
await _pipeline(context);

// Convert IServiceResponse → HttpContext
httpContext.Response.StatusCode = context.Response.StatusCode;
foreach (var header in context.Response.Headers)
    httpContext.Response.Headers[header.Key] = header.Value;
await context.Response.Body.CopyToAsync(httpContext.Response.Body);
```

**gRPC Adapter:**
```csharp
// Convert gRPC message → IServiceRequest
var stream = new MemoryStream();
ProtoBuf.Serializer.Serialize(stream, grpcRequest);
stream.Position = 0;

var serviceRequest = new ServiceRequest
{
    Path = "PetService.GetPetById",
    Method = "unary",
    Body = stream,  // ⭐ Protobuf as stream
    ContentType = "application/protobuf"
};
```

**AMQP Adapter:**
```csharp
// Convert AMQP message → IServiceRequest
var serviceRequest = new ServiceRequest
{
    Path = basicDeliverEventArgs.RoutingKey,
    Method = "request",
    Body = new MemoryStream(basicDeliverEventArgs.Body.ToArray()),
    ContentType = "application/msgpack"
};
```

### Pipeline Component Order

**Correct order:**

1. ✅ **Request Logging** - Log metadata (path, headers)
2. ✅ **Telemetry** - Start trace
3. ✅ **Routing** - Match endpoint ⭐ BEFORE deserialization
4. ✅ **Deserialization** - Stream → DTO (now we know type)
5. ✅ **Validation** - DTO → Validated model
6. ✅ **Execution** - Invoke handler
7. ✅ **Serialization** - Result → Stream
8. ✅ **Response Logging** - Log metadata
9. ✅ **Telemetry** - Complete trace

## Migration Guide

### If you were using the old model:

**Before:**
```csharp
context.RawInput = httpBody;
context.SerializedResult = jsonBytes;
context.StatusCode = 200;
```

**After:**
```csharp
context.Request = new ServiceRequest { Body = httpBodyStream };
context.Response.Body = resultStream;
context.Response.StatusCode = 200;
```

## Benefits Summary

| Aspect | Before | After |
|--------|--------|-------|
| **Type Safety** | ❌ Deserialize blindly | ✅ Know type after routing |
| **Efficiency** | ❌ Deserialize even if route fails | ✅ Only deserialize valid routes |
| **Flexibility** | ❌ Single format per pipeline | ✅ Per-endpoint format negotiation |
| **Observability** | ❌ Must deserialize to log | ✅ Log metadata without deserializing |
| **Security** | ❌ Deserialize untrusted input | ✅ Auth check before deserialization |
| **Memory** | ❌ Allocate DTO objects early | ✅ Defer allocation until needed |

## Files Changed

- ✅ [IServiceRequest.cs](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IServiceRequest.cs) - NEW
- ✅ [IServiceResponse.cs](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IServiceResponse.cs) - NEW
- ✅ [IExecutionContext.cs](../src/dotnet/Polydigm/Polydigm.Execution.Abstractions/IExecutionContext.cs) - UPDATED
- ✅ [execution-pipeline.md](execution-pipeline.md) - UPDATED (flow diagram, examples)

## Next Steps

When implementing pipeline components:

1. **Request Logging** - Access `context.Request.Path`, `context.Request.Headers` (not body)
2. **Routing** - Match `context.Request.Path` + `context.Request.Method`, set `context.Endpoint`
3. **Deserialization** - Read `context.Request.Body` stream, deserialize to type from `context.Endpoint`
4. **Validation** - Convert `context.DeserializedInput` → `context.ValidatedInput`
5. **Execution** - Invoke handler with `context.ValidatedInput`
6. **Serialization** - Write `context.Result` to `context.Response.Body` stream

This refactoring creates a **cleaner separation of concerns** and makes the pipeline more **efficient and secure**! 🚀
