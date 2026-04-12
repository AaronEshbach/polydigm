using Microsoft.AspNetCore.Http;
using Polydigm.Pipeline;
using PipelineExecutionContext = Polydigm.Pipeline.ServiceExecutionContext;

namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// ASP.NET Core middleware that bridges incoming HTTP requests into the Polydigm execution pipeline.
    ///
    /// Lifecycle per request:
    ///   1. Converts HttpRequest → IServiceRequest (body stays as stream, no deserialization)
    ///   2. Bridges ClaimsPrincipal → IAuthenticationContext
    ///   3. Executes the Polydigm pipeline
    ///   4a. If no endpoint was matched and pass-through is enabled: calls the next ASP.NET Core middleware
    ///   4b. Otherwise: writes the IServiceResponse back to HttpResponse
    /// </summary>
    public sealed class PolydigmMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly PipelineDelegate<PipelineExecutionContext> _pipeline;
        private readonly bool _passThrough;

        /// <param name="next">The next ASP.NET Core middleware in the chain.</param>
        /// <param name="pipeline">The built Polydigm pipeline to run per request.</param>
        /// <param name="passThrough">
        /// When true (default), requests that don't match any endpoint are forwarded to
        /// the next ASP.NET Core middleware rather than returning 404.
        /// Useful for running Polydigm alongside static files, health checks, etc.
        /// </param>
        public PolydigmMiddleware(
            RequestDelegate next,
            PipelineDelegate<PipelineExecutionContext> pipeline,
            bool passThrough = true)
        {
            _next = next;
            _pipeline = pipeline;
            _passThrough = passThrough;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var context = new PipelineExecutionContext
            {
                Request = new HttpServiceRequest(httpContext.Request),
                Services = httpContext.RequestServices,
                CancellationToken = httpContext.RequestAborted,
                User = new ClaimsPrincipalAuthenticationContext(httpContext.User)
            };

            await _pipeline(context);

            // No endpoint was matched — let other middleware handle it
            if (_passThrough && context.Endpoint is null && !context.HasError)
            {
                await _next(httpContext);
                return;
            }

            await WriteResponseAsync(httpContext, context);
        }

        private static async Task WriteResponseAsync(HttpContext httpContext, PipelineExecutionContext context)
        {
            var httpResponse = httpContext.Response;
            var serviceResponse = context.Response;

            if (context.HasError)
            {
                httpResponse.StatusCode = serviceResponse.StatusCode is > 0 and not 200
                    ? serviceResponse.StatusCode
                    : 500;

                httpResponse.ContentType = "application/json";
                var errorJson = $"{{\"error\":\"{EscapeJson(context.Error?.Message ?? "An unexpected error occurred")}\"}}";
                await httpResponse.WriteAsync(errorJson, httpContext.RequestAborted);
                return;
            }

            httpResponse.StatusCode = serviceResponse.StatusCode > 0 ? serviceResponse.StatusCode : 200;

            foreach (var (key, value) in serviceResponse.Headers)
                httpResponse.Headers[key] = value;

            if (!string.IsNullOrEmpty(serviceResponse.ContentType))
                httpResponse.ContentType = serviceResponse.ContentType;

            if (serviceResponse.Body is { } body)
            {
                if (body.CanSeek)
                    body.Position = 0;

                await body.CopyToAsync(httpResponse.Body, httpContext.RequestAborted);
            }
        }

        private static string EscapeJson(string value) =>
            value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
