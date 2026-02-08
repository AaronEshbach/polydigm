# Endpoint Metadata

Polydigm's endpoint metadata model provides a **protocol-agnostic** representation of API operations. This allows the same semantic endpoint definition to be implemented across multiple protocols: HTTP REST, gRPC, SOAP, AMQP, GraphQL, etc.

## Core Concepts

### Protocol Independence

The metadata model captures the **semantic essence** of an operation without coupling to protocol-specific details:

- **What** the operation does (query data, create entity, update entity, etc.)
- **What** data it accepts (inputs/parameters)
- **What** data it returns (outputs/responses)
- **How** it behaves (safe, idempotent, requires auth, etc.)

Protocol-specific details (HTTP verbs, paths, status codes, gRPC service names, etc.) are stored in `Extensions` dictionaries and interpreted by protocol-specific generators.

## Metadata Model

### IServiceMetadata

Represents a complete API/service:

```csharp
public interface IServiceMetadata
{
    string Name { get; }                              // "Pet Store API"
    string? Version { get; }                          // "1.0.0"
    string? Description { get; }
    IReadOnlyList<IEndpointMetadata> Endpoints { get; }   // All operations
    IReadOnlyList<IDataType> DataTypes { get; }           // Validated primitives
    IReadOnlyList<IModelMetadata> Models { get; }         // Complex types
    IReadOnlyDictionary<string, object>? Extensions { get; }  // Protocol-specific
}
```

### IEndpointMetadata

Represents a single operation/endpoint:

```csharp
public interface IEndpointMetadata
{
    string Name { get; }                          // "GetPetById"
    string Path { get; }                          // "/pets/{petId}" (canonical address)
    string? Description { get; }                  // "Retrieves a specific pet..."
    IReadOnlyList<IInputParameter> Inputs { get; }       // What goes in
    IReadOnlyList<IOutputResponse> Outputs { get; }      // What comes out
    IEndpointSemantics Semantics { get; }                // Behavioral characteristics
    IReadOnlyDictionary<string, object>? Extensions { get; }  // Protocol hints
}
```

#### Endpoint Path - Canonical Addressing

The `Path` property is the **canonical, protocol-agnostic address** for an endpoint within the service. This enables endpoints to reference each other without using protocol-specific URLs:

```csharp
// Instead of HTTP-specific:
var url = "https://api.example.com/pets/PET-123456";

// Use protocol-agnostic path:
var endpoint = service.GetEndpointByPath("/pets/{petId}");
```

**Path formats by protocol:**
- **REST**: `/pets/{petId}`, `/orders/{id}/items`
- **gRPC**: `PetService.GetPetById`, `OrderService.CreateOrder`
- **AMQP**: `pets.get`, `orders.create`, `notifications.send`
- **GraphQL**: `Query.getPetById`, `Mutation.createOrder`

The path must be **unique within the service** - no two endpoints can share the same path.

### IInputParameter

Represents an input to an operation:

```csharp
public interface IInputParameter
{
    string Name { get; }                  // "petId"
    IDataType DataType { get; }          // PetId (validated type)
    bool IsRequired { get; }             // true
    InputParameterKind Kind { get; }     // Path, Query, Body, Header, Context
    string? Description { get; }
    object? DefaultValue { get; }
}
```

**InputParameterKind** provides hints about parameter semantics:
- `Path`: Part of the endpoint identifier (e.g., `/pets/{petId}`)
- `Query`: Filtering/options (e.g., `?limit=10`)
- `Body`: Main request payload
- `Header`: Metadata (e.g., authorization, content-type)
- `Context`: Injected by infrastructure (e.g., current user)

### IOutputResponse

Represents a possible output from an operation:

```csharp
public interface IOutputResponse
{
    string Name { get; }                  // "200" or "Success" or "NotFound"
    string? Description { get; }          // "Pet found successfully"
    IDataType? DataType { get; }         // Pet model (null for empty responses)
    OutputKind Kind { get; }             // Success, ClientError, ServerError
    IReadOnlyDictionary<string, object>? Extensions { get; }
}
```

### IEndpointSemantics

Behavioral characteristics:

```csharp
public interface IEndpointSemantics
{
    OperationIntent Intent { get; }           // Query, Create, Update, Delete, Action, Event
    bool IsIdempotent { get; }               // Can be called multiple times safely
    bool IsSafe { get; }                     // Read-only, no side effects
    bool RequiresAuthentication { get; }
    bool IsDeprecated { get; }
    IReadOnlyList<string> Tags { get; }      // ["pets", "admin"]
}
```

**OperationIntent** describes the semantic purpose:
- `Query`: Read operation (safe, idempotent) - maps to HTTP GET, gRPC Get*, GraphQL query
- `Create`: Create new entity - maps to HTTP POST, gRPC Create*
- `Update`: Modify existing entity - maps to HTTP PUT/PATCH, gRPC Update*
- `Delete`: Remove entity - maps to HTTP DELETE, gRPC Delete*
- `Action`: Generic command - maps to HTTP POST, custom RPC
- `Event`: Async notification - maps to AsyncAPI, AMQP, gRPC stream

## Protocol Mapping Examples

### HTTP REST

```csharp
Endpoint: GetPetById
  Intent: Query
  Inputs:
    - petId (Path, required): PetId
  Outputs:
    - 200 (Success): Pet
    - 404 (ClientError): void
  Extensions:
    - http-method: "GET"
    - http-path: "/pets/{petId}"

→ Maps to: GET /pets/{petId}
```

### gRPC

```csharp
Endpoint: GetPetById
  Intent: Query
  Inputs:
    - petId (Path, required): PetId
  Outputs:
    - 200 (Success): Pet
    - 404 (ClientError): void

→ Maps to:
service PetService {
  rpc GetPetById(GetPetByIdRequest) returns (Pet);
}
message GetPetByIdRequest {
  string petId = 1;
}
```

### GraphQL

```csharp
Endpoint: GetPetById
  Intent: Query
  Inputs:
    - petId (Path, required): PetId
  Outputs:
    - 200 (Success): Pet

→ Maps to:
type Query {
  getPetById(petId: String!): Pet
}
```

### AMQP

```csharp
Endpoint: GetPetById
  Intent: Query
  Inputs:
    - petId (Body, required): PetId
  Outputs:
    - 200 (Success): Pet

→ Maps to:
Queue: pets.get
Request Message: { "petId": "PET-123456" }
Response Message: { "id": "PET-123456", "name": "Fluffy", ... }
```

## Usage Examples

### Extracting Endpoints from OpenAPI

```csharp
var processor = new OpenApiSpecificationProcessor();
var source = SpecificationSource.FromFile("api.yaml");
var result = await processor.ProcessAsync(source);

// Access service metadata
var service = result.ServiceMetadata;
Console.WriteLine($"{service.Name} v{service.Version}");

// Iterate endpoints
foreach (var endpoint in result.Endpoints)
{
    Console.WriteLine($"{endpoint.Name}: {endpoint.Description}");
    Console.WriteLine($"  Intent: {endpoint.Semantics.Intent}");
    Console.WriteLine($"  Safe: {endpoint.Semantics.IsSafe}");
    Console.WriteLine($"  Idempotent: {endpoint.Semantics.IsIdempotent}");

    foreach (var input in endpoint.Inputs)
    {
        var req = input.IsRequired ? "required" : "optional";
        Console.WriteLine($"  Input: {input.Name} ({input.Kind}): {input.DataType.Name} ({req})");
    }

    foreach (var output in endpoint.Outputs)
    {
        var type = output.DataType?.Name ?? "void";
        Console.WriteLine($"  Output: {output.Name} ({output.Kind}): {type}");
    }
}
```

### Protocol-Specific Code Generation

Future generators can consume the same endpoint metadata and produce different implementations:

```csharp
// HTTP REST API (ASP.NET Core)
var httpGenerator = new AspNetCoreEndpointGenerator();
var controllers = httpGenerator.GenerateAll(service.Endpoints);

// gRPC Service
var grpcGenerator = new GrpcServiceGenerator();
var protoFiles = grpcGenerator.GenerateAll(service.Endpoints);

// GraphQL Schema
var graphqlGenerator = new GraphQLSchemaGenerator();
var schema = graphqlGenerator.GenerateAll(service.Endpoints);

// AMQP Consumer
var amqpGenerator = new AmqpConsumerGenerator();
var consumers = amqpGenerator.GenerateAll(service.Endpoints);
```

## Internal Endpoint Addressing

Endpoints can reference other endpoints using their canonical path instead of protocol-specific URLs. This enables:

### 1. Service-to-Service Communication

```csharp
// Look up endpoint by path
var createOrderEndpoint = service.GetEndpointByPath("/orders");
var getOrderEndpoint = service.GetEndpointByPath("/orders/{orderId}");

// Generate protocol-specific client code
var client = CreateClient(createOrderEndpoint);
var order = await client.InvokeAsync(orderData);
```

### 2. Endpoint Composition

```csharp
// Define a composite operation that calls multiple endpoints
public class CreatePetWithOwnerOperation
{
    private readonly IServiceMetadata _service;

    public async Task<Pet> ExecuteAsync(PetData petData, OwnerData ownerData)
    {
        // Call CreateOwner endpoint
        var createOwner = _service.GetEndpointByPath("/owners");
        var owner = await InvokeEndpoint(createOwner, ownerData);

        // Call CreatePet endpoint
        var createPet = _service.GetEndpointByPath("/pets");
        var pet = await InvokeEndpoint(createPet, petData with { OwnerId = owner.Id });

        return pet;
    }
}
```

### 3. Protocol-Agnostic Workflow Orchestration

```csharp
// Define workflows using endpoint paths
var workflow = new Workflow
{
    Steps = new[]
    {
        new WorkflowStep { Endpoint = "/orders", Input = "orderData" },
        new WorkflowStep { Endpoint = "/payments", Input = "paymentData" },
        new WorkflowStep { Endpoint = "/notifications/send", Input = "confirmationData" }
    }
};

// Execute workflow regardless of underlying protocol
await ExecuteWorkflow(workflow);
```

### 4. API Gateway Routing

```csharp
// Configure gateway routes using endpoint paths
var gateway = new ApiGateway(service);

// All these map to the same canonical endpoint: "/pets/{petId}"
gateway.MapRoute("GET", "/api/v1/pets/{petId}",  endpoint: "/pets/{petId}");
gateway.MapRoute("gRPC", "PetService.GetPetById", endpoint: "/pets/{petId}");
gateway.MapRoute("AMQP", "pets.get",              endpoint: "/pets/{petId}");
```

## Benefits

1. **Single Source of Truth**: Define your API once, generate for multiple protocols
2. **Protocol Migration**: Easily migrate from REST to gRPC or GraphQL
3. **Consistency**: Ensure the same semantics across all protocol implementations
4. **Tooling**: Generate clients, servers, docs, tests from the same metadata
5. **Evolution**: Add new protocols without changing existing definitions
6. **Internal Addressing**: Endpoints reference each other without protocol coupling
7. **Workflow Orchestration**: Build complex workflows across protocol boundaries

## Next Steps

- Implement ASP.NET Core controller generator
- Implement gRPC service generator
- Implement GraphQL schema generator
- Add endpoint validation and testing utilities
- Create endpoint documentation generator
