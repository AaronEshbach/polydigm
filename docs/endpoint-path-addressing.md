# Endpoint Path-Based Addressing

## Overview

Every endpoint in Polydigm has a **unique canonical path** that serves as its internal address within the framework. This enables protocol-agnostic endpoint references and composition without coupling to HTTP URLs, gRPC service names, or other protocol-specific identifiers.

## The Path Property

```csharp
public interface IEndpointMetadata
{
    string Name { get; }        // Human-readable name: "GetPetById"
    string Path { get; }        // Canonical address: "/pets/{petId}"
    // ... other properties
}
```

### Requirements

1. **Uniqueness**: No two endpoints in a service can share the same path
2. **Stability**: The path should not change when mapping to different protocols
3. **Descriptive**: Should clearly indicate what the endpoint addresses
4. **Protocol-Agnostic**: Should work across REST, gRPC, AMQP, GraphQL, etc.

## Path Formats by Protocol

### HTTP REST
Paths are derived from the HTTP path template:

```
GET    /pets              → Path: "/pets"
POST   /pets              → Path: "/pets"
GET    /pets/{petId}      → Path: "/pets/{petId}"
PUT    /pets/{petId}      → Path: "/pets/{petId}"
DELETE /pets/{petId}      → Path: "/pets/{petId}"
```

**Note**: Different HTTP methods on the same path will share the same canonical path. The `Name` property differentiates them (e.g., "ListPets", "CreatePet", "GetPet", "UpdatePet", "DeletePet").

### gRPC
Paths follow the `Service.Method` convention:

```
service PetService {
  rpc GetPetById(...)      → Path: "PetService.GetPetById"
  rpc CreatePet(...)       → Path: "PetService.CreatePet"
  rpc ListPets(...)        → Path: "PetService.ListPets"
}
```

### AMQP (Message Queue)
Paths use routing key or queue name format:

```
Queue: pets.get            → Path: "pets.get"
Queue: pets.create         → Path: "pets.create"
Queue: orders.process      → Path: "orders.process"
```

### GraphQL
Paths use `OperationType.fieldName` format:

```
type Query {
  getPetById(...)          → Path: "Query.getPetById"
  listPets(...)            → Path: "Query.listPets"
}

type Mutation {
  createPet(...)           → Path: "Mutation.createPet"
}
```

## Usage Examples

### 1. Endpoint Lookup

```csharp
var service = await LoadServiceMetadata("api.yaml");

// Look up by canonical path
var endpoint = service.GetEndpointByPath("/pets/{petId}");

// Or look up by name
var endpoint = service.GetEndpointByName("GetPetById");

Console.WriteLine($"Endpoint: {endpoint.Name}");
Console.WriteLine($"Path: {endpoint.Path}");
Console.WriteLine($"Intent: {endpoint.Semantics.Intent}");
```

### 2. Endpoint Composition

Build workflows that chain endpoints together:

```csharp
public async Task<Pet> CreatePetWithOwner(PetData petData, OwnerData ownerData)
{
    var service = GetServiceMetadata();

    // Look up endpoints by path (protocol-agnostic)
    var createOwner = service.GetEndpointByPath("/owners");
    var createPet = service.GetEndpointByPath("/pets");

    // Invoke endpoints
    var owner = await InvokeEndpoint(createOwner, ownerData);
    var pet = await InvokeEndpoint(createPet, petData with { OwnerId = owner.Id });

    return pet;
}
```

### 3. Multi-Protocol Routing

Map different protocol requests to the same canonical endpoint:

```csharp
public class ApiGateway
{
    private readonly IServiceMetadata _service;

    public async Task<TResponse> RouteRequest<TRequest, TResponse>(
        string protocol,
        string protocolAddress,
        TRequest request)
    {
        // Map protocol-specific address to canonical path
        var canonicalPath = MapToCanonicalPath(protocol, protocolAddress);

        // Look up endpoint
        var endpoint = _service.GetEndpointByPath(canonicalPath);
        if (endpoint == null)
            throw new EndpointNotFoundException(canonicalPath);

        // Invoke using appropriate protocol handler
        return await InvokeEndpoint<TRequest, TResponse>(endpoint, request);
    }

    private string MapToCanonicalPath(string protocol, string address)
    {
        return protocol switch
        {
            "http" => address,                          // "/pets/PET-123456"
            "grpc" => ConvertGrpcPath(address),        // "PetService.GetPetById" → "/pets/{petId}"
            "amqp" => ConvertAmqpPath(address),        // "pets.get" → "/pets/{petId}"
            _ => throw new UnsupportedProtocolException(protocol)
        };
    }
}
```

### 4. Workflow Orchestration

Define workflows using canonical paths:

```csharp
var workflow = new Workflow
{
    Name = "ProcessOrder",
    Steps = new[]
    {
        new WorkflowStep
        {
            Name = "CreateOrder",
            Endpoint = "/orders",                    // Canonical path
            Input = "{{ request.orderData }}"
        },
        new WorkflowStep
        {
            Name = "ProcessPayment",
            Endpoint = "/payments",                  // Canonical path
            Input = "{{ steps.CreateOrder.output }}"
        },
        new WorkflowStep
        {
            Name = "SendConfirmation",
            Endpoint = "/notifications/send",        // Canonical path
            Input = "{{ steps.ProcessPayment.output }}"
        }
    }
};

// Execute workflow - works with any protocol implementation
await _orchestrator.ExecuteWorkflow(workflow);
```

### 5. Service Mesh / API Gateway Configuration

```yaml
# Gateway routes - all map to same canonical endpoint
routes:
  - protocol: http
    pattern: "GET /api/v1/pets/{petId}"
    endpoint: "/pets/{petId}"

  - protocol: grpc
    pattern: "PetService.GetPetById"
    endpoint: "/pets/{petId}"

  - protocol: amqp
    pattern: "pets.get"
    endpoint: "/pets/{petId}"

# Single endpoint definition, multiple protocol implementations
# Consistent semantics, validation, and behavior across all protocols
```

## Path Uniqueness

The framework enforces path uniqueness within a service:

```csharp
var service = LoadServiceMetadata("api.yaml");

// Verify uniqueness
var paths = service.Endpoints.Select(e => e.Path).ToList();
var uniquePaths = paths.Distinct().Count();

if (uniquePaths != service.Endpoints.Count)
{
    var duplicates = paths.GroupBy(p => p)
        .Where(g => g.Count() > 1)
        .Select(g => g.Key);

    throw new DuplicateEndpointPathException(duplicates);
}
```

## REST API Special Case

In REST APIs, multiple endpoints can share the same HTTP path but differ by HTTP method:

```
GET    /pets      → Path: "/pets", Name: "ListPets"
POST   /pets      → Path: "/pets", Name: "CreatePet"
```

While they share the same canonical path `/pets`, they are distinguished by:
- **Name**: Different operation names
- **Semantics.Intent**: Different operation intents (Query vs Create)
- **Extensions["http-method"]**: Different HTTP methods

When converting to other protocols, these become separate operations:
- gRPC: `PetService.ListPets` and `PetService.CreatePet`
- AMQP: `pets.list` and `pets.create`

## Benefits

1. **Protocol Independence**: Reference endpoints without knowing the protocol
2. **Workflow Portability**: Move workflows between REST, gRPC, AMQP seamlessly
3. **Service Composition**: Build higher-level services from endpoint primitives
4. **Gateway Routing**: Single routing table for multi-protocol gateways
5. **Testing**: Test endpoint logic independent of protocol bindings
6. **Documentation**: Generate unified docs across all protocol implementations

## Implementation

The `Path` property is automatically populated when extracting from specifications:

```csharp
// OpenAPI extraction
var processor = new OpenApiSpecificationProcessor();
var result = await processor.ProcessAsync(source);

foreach (var endpoint in result.Endpoints)
{
    Console.WriteLine($"{endpoint.Path} → {endpoint.Name}");
    // Output:
    // /pets → ListPets
    // /pets → CreatePet
    // /pets/{petId} → GetPet
    // /pets/{petId} → UpdatePet
    // /pets/{petId} → DeletePet
}
```

For other specification formats (AsyncAPI, gRPC proto, etc.), the extractor should populate the `Path` property according to that protocol's conventions, ensuring uniqueness within the service.
