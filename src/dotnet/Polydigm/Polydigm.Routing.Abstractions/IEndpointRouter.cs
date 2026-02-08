using Polydigm.Metadata;
using Polydigm.Pipeline;

namespace Polydigm.Routing
{
    /// <summary>
    /// Routes requests to the appropriate endpoint based on the execution context.
    /// Different implementations for HTTP routing, gRPC service dispatch, AMQP routing, etc.
    /// </summary>
    public interface IEndpointRouter
    {
        /// <summary>
        /// Routes the request to an endpoint based on the execution context.
        /// </summary>
        /// <param name="context">The execution context.</param>
        /// <returns>The matched endpoint metadata, or null if no match found.</returns>
        Task<IEndpointMetadata?> RouteAsync(IExecutionContext context);
    }

    /// <summary>
    /// Routing result with match information.
    /// </summary>
    public sealed class RoutingResult
    {
        /// <summary>
        /// Whether a route was matched.
        /// </summary>
        public bool IsMatch { get; init; }

        /// <summary>
        /// The matched endpoint metadata.
        /// </summary>
        public IEndpointMetadata? Endpoint { get; init; }

        /// <summary>
        /// Route parameters extracted from the path (e.g., {petId} → "PET-123456").
        /// </summary>
        public IDictionary<string, object> RouteParameters { get; init; } = new Dictionary<string, object>();

        /// <summary>
        /// Query parameters from the request.
        /// </summary>
        public IDictionary<string, object> QueryParameters { get; init; } = new Dictionary<string, object>();

        public static RoutingResult Success(IEndpointMetadata endpoint)
            => new() { IsMatch = true, Endpoint = endpoint };

        public static RoutingResult NotFound()
            => new() { IsMatch = false };
    }
}
