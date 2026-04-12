using Polydigm.Metadata;
using Polydigm.Pipeline;

namespace Polydigm.Hosting.AspNetCore
{
    /// <summary>
    /// Associates an endpoint's metadata descriptor with its handler function.
    /// The handler receives the execution context and returns the result to serialize.
    /// </summary>
    public sealed class HttpEndpointRegistration
    {
        public required IEndpointMetadata Metadata { get; init; }
        public required Func<IExecutionContext, Task<object?>> Handler { get; init; }
    }

    /// <summary>
    /// Registry of all HTTP endpoints in the application.
    /// Populated at startup either manually (via Map/MapGet/etc.) or automatically
    /// via service discovery (AddPolydigmHttpServices). Consumed at request time by
    /// HttpRoutingComponent and HttpExecutionComponent.
    ///
    /// Registered as a singleton in DI.
    /// </summary>
    public sealed class HttpEndpointRegistry
    {
        private readonly List<HttpEndpointRegistration> _registrations = new();

        public IReadOnlyList<HttpEndpointRegistration> Registrations => _registrations;

        /// <summary>
        /// Registers an endpoint with an explicit HTTP method, path template, and handler function.
        /// </summary>
        public void Map(
            string httpMethod,
            string path,
            string name,
            Func<IExecutionContext, Task<object?>> handler,
            OperationIntent intent = OperationIntent.Action,
            string? description = null)
        {
            var metadata = new EndpointMetadata
            {
                Name = name,
                Path = path,
                Description = description,
                Semantics = new EndpointSemantics
                {
                    Intent = intent,
                    IsSafe = intent == OperationIntent.Query,
                    IsIdempotent = intent is OperationIntent.Query or OperationIntent.Update or OperationIntent.Delete
                },
                Extensions = new Dictionary<string, object>
                {
                    [HttpExtensionKeys.Method] = httpMethod.ToUpperInvariant()
                }
            };

            _registrations.Add(new HttpEndpointRegistration { Metadata = metadata, Handler = handler });
        }

        internal void Register(IEndpointMetadata metadata, Func<IExecutionContext, Task<object?>> handler)
        {
            _registrations.Add(new HttpEndpointRegistration { Metadata = metadata, Handler = handler });
        }

        public void MapGet(string path, string name, Func<IExecutionContext, Task<object?>> handler, string? description = null)
            => Map("GET", path, name, handler, OperationIntent.Query, description);

        public void MapPost(string path, string name, Func<IExecutionContext, Task<object?>> handler, string? description = null)
            => Map("POST", path, name, handler, OperationIntent.Action, description);

        public void MapPut(string path, string name, Func<IExecutionContext, Task<object?>> handler, string? description = null)
            => Map("PUT", path, name, handler, OperationIntent.Update, description);

        public void MapDelete(string path, string name, Func<IExecutionContext, Task<object?>> handler, string? description = null)
            => Map("DELETE", path, name, handler, OperationIntent.Delete, description);
    }
}
