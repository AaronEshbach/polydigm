using Polydigm.Specifications;
using Polydigm.Specifications.OpenApi;
using System;
using System.Threading.Tasks;

namespace Polydigm.Samples
{
    /// <summary>
    /// Demonstrates how endpoints use canonical paths for internal addressing,
    /// allowing protocol-agnostic endpoint references.
    /// </summary>
    class EndpointPathExample
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Endpoint Path-Based Addressing Example ===");
            Console.WriteLine();

            // Parse OpenAPI specification
            var processor = new OpenApiSpecificationProcessor();
            var source = SpecificationSource.FromFile("samples/sample-api.yaml");
            var result = await processor.ProcessAsync(source);

            var service = result.ServiceMetadata;
            if (service == null)
            {
                Console.WriteLine("No service metadata found");
                return;
            }

            Console.WriteLine($"Service: {service.Name} v{service.Version}");
            Console.WriteLine($"Endpoints: {service.Endpoints.Count}");
            Console.WriteLine();

            // Demonstrate canonical path lookup
            Console.WriteLine("=== Endpoint Path Lookup ===");
            Console.WriteLine();

            // Look up endpoint by path (protocol-agnostic)
            var getPetEndpoint = service.GetEndpointByPath("/pets/{petId}");
            if (getPetEndpoint != null)
            {
                Console.WriteLine($"Found endpoint: {getPetEndpoint.Name}");
                Console.WriteLine($"  Canonical Path: {getPetEndpoint.Path}");
                Console.WriteLine($"  Intent: {getPetEndpoint.Semantics.Intent}");
                Console.WriteLine($"  Safe: {getPetEndpoint.Semantics.IsSafe}");
                Console.WriteLine($"  Idempotent: {getPetEndpoint.Semantics.IsIdempotent}");

                // Access protocol-specific details from extensions
                if (getPetEndpoint.Extensions != null)
                {
                    if (getPetEndpoint.Extensions.TryGetValue("http-method", out var httpMethod) &&
                        getPetEndpoint.Extensions.TryGetValue("http-path", out var httpPath))
                    {
                        Console.WriteLine($"  HTTP Mapping: {httpMethod} {httpPath}");
                    }
                }
                Console.WriteLine();
            }

            // Demonstrate endpoint composition scenario
            Console.WriteLine("=== Endpoint Composition Scenario ===");
            Console.WriteLine();
            Console.WriteLine("Imagine a workflow: Create Pet → Get Pet Details");
            Console.WriteLine();

            var createPet = service.GetEndpointByPath("/pets");
            var getPet = service.GetEndpointByPath("/pets/{petId}");

            if (createPet != null && getPet != null)
            {
                Console.WriteLine($"Step 1: Call {createPet.Path}");
                Console.WriteLine($"  Operation: {createPet.Name} ({createPet.Semantics.Intent})");
                Console.WriteLine($"  Returns: Pet with ID");
                Console.WriteLine();

                Console.WriteLine($"Step 2: Call {getPet.Path}");
                Console.WriteLine($"  Operation: {getPet.Name} ({getPet.Semantics.Intent})");
                Console.WriteLine($"  Input: Use Pet ID from Step 1");
                Console.WriteLine($"  Returns: Full Pet details");
                Console.WriteLine();

                Console.WriteLine("This workflow works regardless of protocol:");
                Console.WriteLine("  - REST: POST /pets → GET /pets/{petId}");
                Console.WriteLine("  - gRPC: CreatePet → GetPetById");
                Console.WriteLine("  - AMQP: pets.create → pets.get");
            }

            Console.WriteLine();
            Console.WriteLine("=== All Endpoint Paths ===");
            Console.WriteLine();

            foreach (var endpoint in service.Endpoints)
            {
                var httpMethod = endpoint.Extensions?.TryGetValue("http-method", out var method) == true
                    ? method.ToString()
                    : "N/A";

                Console.WriteLine($"{endpoint.Path,-30} {endpoint.Name,-20} ({httpMethod})");
            }

            Console.WriteLine();
            Console.WriteLine("=== Endpoint Uniqueness Check ===");
            Console.WriteLine();

            // Verify all paths are unique
            var paths = service.Endpoints.Select(e => e.Path).ToList();
            var uniquePaths = paths.Distinct().Count();

            Console.WriteLine($"Total endpoints: {service.Endpoints.Count}");
            Console.WriteLine($"Unique paths: {uniquePaths}");
            Console.WriteLine($"All paths unique: {uniquePaths == service.Endpoints.Count}");

            if (uniquePaths != service.Endpoints.Count)
            {
                Console.WriteLine();
                Console.WriteLine("⚠️ WARNING: Duplicate paths detected!");
                var duplicates = paths.GroupBy(p => p)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                foreach (var dup in duplicates)
                {
                    Console.WriteLine($"  Duplicate: {dup}");
                }
            }
            else
            {
                Console.WriteLine("✅ All endpoint paths are unique");
            }
        }
    }
}
