using System.Text.Json;
using Polydigm.Pipeline;
using ExecutionContext = Polydigm.Pipeline.ServiceExecutionContext;

namespace Polydigm.Hosting.AspNetCore.Components
{
    /// <summary>
    /// Serializes context.Result to JSON and writes it to context.Response.Body.
    /// Runs after execution so the result is available.
    ///
    /// A null result produces a 204 No Content response.
    /// A non-null result is serialized using System.Text.Json and sets the
    /// Content-Type to "application/json".
    /// </summary>
    public sealed class HttpSerializationComponent : IPipelineComponent<ExecutionContext>
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public async Task InvokeAsync(ExecutionContext context, PipelineDelegate<ExecutionContext> next)
        {
            // Let the rest of the pipeline run first
            await next(context);

            // Don't overwrite an error response
            if (context.HasError)
                return;

            // No endpoint matched — nothing to serialize
            if (context.Endpoint is null)
                return;

            if (context.Result is null)
            {
                context.Response.StatusCode = 204;
                return;
            }

            var body = new MemoryStream();
            await JsonSerializer.SerializeAsync(body, context.Result, context.Result.GetType(), JsonOptions, context.CancellationToken);

            context.Response.Body = body;
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
        }
    }
}
