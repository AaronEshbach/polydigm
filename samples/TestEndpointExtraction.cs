using Polydigm.Specifications;
using Polydigm.Specifications.OpenApi;
using System;
using System.Threading.Tasks;

namespace Polydigm.Samples
{
    class TestEndpointExtraction
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Polydigm Endpoint Extraction Test ===");
            Console.WriteLine();

            var processor = new OpenApiSpecificationProcessor();
            var source = SpecificationSource.FromFile("samples/sample-api.yaml");

            var result = await processor.ProcessAsync(source);

            Console.WriteLine($"Service: {result.ServiceMetadata?.Name} v{result.ServiceMetadata?.Version}");
            Console.WriteLine($"Description: {result.ServiceMetadata?.Description}");
            Console.WriteLine();

            Console.WriteLine($"Found {result.DataTypes.Count} data types");
            Console.WriteLine($"Found {result.Models.Count} models");
            Console.WriteLine($"Found {result.Endpoints.Count} endpoints");
            Console.WriteLine();

            Console.WriteLine("=== Endpoints ===");
            foreach (var endpoint in result.Endpoints)
            {
                Console.WriteLine();
                Console.WriteLine($"  {endpoint.Name}");
                Console.WriteLine($"    Description: {endpoint.Description}");
                Console.WriteLine($"    Intent: {endpoint.Semantics.Intent}");
                Console.WriteLine($"    Safe: {endpoint.Semantics.IsSafe}, Idempotent: {endpoint.Semantics.IsIdempotent}");

                if (endpoint.Extensions != null)
                {
                    if (endpoint.Extensions.TryGetValue("http-method", out var method) &&
                        endpoint.Extensions.TryGetValue("http-path", out var path))
                    {
                        Console.WriteLine($"    HTTP: {method} {path}");
                    }
                }

                if (endpoint.Inputs.Count > 0)
                {
                    Console.WriteLine($"    Inputs:");
                    foreach (var input in endpoint.Inputs)
                    {
                        var required = input.IsRequired ? "required" : "optional";
                        Console.WriteLine($"      - {input.Name} ({input.Kind}): {input.DataType.Name} ({required})");
                    }
                }

                if (endpoint.Outputs.Count > 0)
                {
                    Console.WriteLine($"    Outputs:");
                    foreach (var output in endpoint.Outputs)
                    {
                        var dataType = output.DataType?.Name ?? "void";
                        Console.WriteLine($"      - {output.Name} ({output.Kind}): {dataType}");
                    }
                }

                if (endpoint.Semantics.Tags.Count > 0)
                {
                    Console.WriteLine($"    Tags: {string.Join(", ", endpoint.Semantics.Tags)}");
                }
            }
        }
    }
}
