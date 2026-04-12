using Polydigm.Pipeline;
using Polydigm.Validation;
using ExecutionContext = Polydigm.Pipeline.ServiceExecutionContext;

namespace Polydigm.Hosting.AspNetCore.Components
{
    /// <summary>
    /// Invokes the registered handler for the matched endpoint and stores the result
    /// in context.Result for the serialization component to pick up.
    ///
    /// Skips execution when no endpoint was matched (routing is expected to run first).
    ///
    /// Exception handling:
    ///   - ValidationException → marks context as errored with status 400 (Bad Request)
    ///   - Any other exception → marks context as errored; middleware writes a 500 response
    /// </summary>
    public sealed class HttpExecutionComponent : IPipelineComponent<ExecutionContext>
    {
        private readonly HttpEndpointRegistry _registry;

        public HttpExecutionComponent(HttpEndpointRegistry registry)
        {
            _registry = registry;
        }

        public async Task InvokeAsync(ExecutionContext context, PipelineDelegate<ExecutionContext> next)
        {
            // No route matched — nothing to execute
            if (context.Endpoint is null)
            {
                await next(context);
                return;
            }

            var registration = _registry.Registrations
                .FirstOrDefault(r => r.Metadata == context.Endpoint);

            if (registration is null)
            {
                context.HasError = true;
                context.Error = new InvalidOperationException(
                    $"No handler registered for endpoint '{context.Endpoint.Name}' at '{context.Endpoint.Path}'.");
                return;
            }

            try
            {
                context.Result = await registration.Handler(context);
            }
            catch (ValidationException ex)
            {
                context.HasError = true;
                context.Error = ex;
                context.Response.StatusCode = 400;
                return;
            }
            catch (Exception ex)
            {
                context.HasError = true;
                context.Error = ex;
                return;
            }

            await next(context);
        }
    }
}
