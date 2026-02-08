using Polydigm.Metadata;

namespace Polydigm.Pipeline
{
    /// <summary>
    /// Represents the execution context for a single request flowing through the pipeline.
    /// Contains the request data, response data, endpoint metadata, and shared properties.
    /// </summary>
    public interface IExecutionContext
    {
        /// <summary>
        /// The protocol-agnostic service request.
        /// Set by the transport adapter before entering the pipeline.
        /// Contains request metadata and body stream (undeserialized).
        /// </summary>
        IServiceRequest Request { get; set; }

        /// <summary>
        /// The protocol-agnostic service response.
        /// Built by the pipeline and converted to wire format by the transport adapter.
        /// Contains response metadata and body stream (serialized).
        /// </summary>
        IServiceResponse Response { get; set; }

        /// <summary>
        /// The endpoint being invoked (populated by routing component).
        /// </summary>
        IEndpointMetadata? Endpoint { get; set; }

        /// <summary>
        /// The authenticated user/principal making the request.
        /// Set by the transport adapter or authentication middleware.
        /// Used by the authorization component to check access.
        /// </summary>
        IAuthenticationContext? User { get; set; }

        /// <summary>
        /// Deserialized input as a DTO (unvalidated).
        /// Set by the deserialization component after routing and authorization.
        /// </summary>
        object? DeserializedInput { get; set; }

        /// <summary>
        /// Validated input model (with all constraints satisfied).
        /// Set by the validation component.
        /// </summary>
        object? ValidatedInput { get; set; }

        /// <summary>
        /// Result from executing the endpoint handler.
        /// Set by the execution component.
        /// </summary>
        object? Result { get; set; }

        /// <summary>
        /// Indicates whether the pipeline has produced an error response.
        /// Components can check this to skip processing or handle errors differently.
        /// </summary>
        bool HasError { get; set; }

        /// <summary>
        /// Error information if HasError is true.
        /// </summary>
        Exception? Error { get; set; }

        /// <summary>
        /// Shared properties bag for passing data between pipeline components.
        /// Example uses: correlation IDs, user identity, feature flags, etc.
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Cancellation token for the request.
        /// Components should respect this and cancel work when triggered.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Service provider for dependency injection within pipeline components.
        /// </summary>
        IServiceProvider? Services { get; }
    }

    /// <summary>
    /// Base implementation of IExecutionContext.
    /// </summary>
    public class ExecutionContext : IExecutionContext
    {
        public IServiceRequest Request { get; set; } = null!;
        public IServiceResponse Response { get; set; } = new ServiceResponse();
        public IEndpointMetadata? Endpoint { get; set; }
        public IAuthenticationContext? User { get; set; }
        public object? DeserializedInput { get; set; }
        public object? ValidatedInput { get; set; }
        public object? Result { get; set; }
        public bool HasError { get; set; }
        public Exception? Error { get; set; }
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();
        public CancellationToken CancellationToken { get; init; }
        public IServiceProvider? Services { get; init; }
    }
}
