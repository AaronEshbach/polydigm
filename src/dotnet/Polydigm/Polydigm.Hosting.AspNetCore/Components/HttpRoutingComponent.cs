using Polydigm.Pipeline;
using ExecutionContext = Polydigm.Pipeline.ServiceExecutionContext;

namespace Polydigm.Hosting.AspNetCore.Components
{
    /// <summary>
    /// Resolves the incoming request to a registered endpoint by matching the request
    /// path and HTTP method against the HttpEndpointRegistry.
    ///
    /// Supports route parameters in path templates (e.g. /items/{id}).
    /// Extracted route parameters are stored in context.Request.RouteParameters.
    ///
    /// If no match is found the pipeline continues with context.Endpoint == null,
    /// which causes the middleware to pass through to the next ASP.NET Core middleware
    /// (or return 404 if pass-through is disabled).
    /// </summary>
    public sealed class HttpRoutingComponent : IPipelineComponent<ExecutionContext>
    {
        private readonly HttpEndpointRegistry _registry;

        public HttpRoutingComponent(HttpEndpointRegistry registry)
        {
            _registry = registry;
        }

        public Task InvokeAsync(ExecutionContext context, PipelineDelegate<ExecutionContext> next)
        {
            foreach (var registration in _registry.Registrations)
            {
                var metadata = registration.Metadata;

                if (!MatchesMethod(metadata, context.Request.Method))
                    continue;

                if (!TryMatchPath(metadata.Path, context.Request.Path, out var routeParams))
                    continue;

                context.Endpoint = metadata;

                foreach (var (key, value) in routeParams)
                    context.Request.RouteParameters[key] = value;

                break;
            }

            return next(context);
        }

        private static bool MatchesMethod(Polydigm.Metadata.IEndpointMetadata metadata, string requestMethod)
        {
            if (metadata.Extensions?.TryGetValue(HttpExtensionKeys.Method, out var methodObj) == true
                && methodObj is string method)
            {
                return method.Equals(requestMethod, StringComparison.OrdinalIgnoreCase);
            }

            // Fall back to inferring from OperationIntent when no explicit method is registered
            var inferred = metadata.Semantics.Intent switch
            {
                Polydigm.Metadata.OperationIntent.Query  => "GET",
                Polydigm.Metadata.OperationIntent.Create => "POST",
                Polydigm.Metadata.OperationIntent.Update => "PUT",
                Polydigm.Metadata.OperationIntent.Delete => "DELETE",
                _                                        => "POST"
            };

            return inferred.Equals(requestMethod, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Matches a path template against an actual request path.
        /// Template segments wrapped in {curly braces} are route parameters.
        /// </summary>
        private static bool TryMatchPath(
            string template,
            string requestPath,
            out Dictionary<string, string> routeParams)
        {
            routeParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            var templateSegments = template.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var requestSegments  = requestPath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (templateSegments.Length != requestSegments.Length)
                return false;

            for (var i = 0; i < templateSegments.Length; i++)
            {
                var templateSeg = templateSegments[i];
                var requestSeg  = requestSegments[i];

                if (templateSeg.StartsWith('{') && templateSeg.EndsWith('}'))
                {
                    var paramName = templateSeg[1..^1];
                    routeParams[paramName] = requestSeg;
                }
                else if (!templateSeg.Equals(requestSeg, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
