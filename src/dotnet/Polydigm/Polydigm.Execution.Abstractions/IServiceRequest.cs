namespace Polydigm.Execution
{
    /// <summary>
    /// Represents a protocol-agnostic service request.
    /// Transport adapters convert from wire format (HTTP, gRPC, AMQP) to this model.
    /// The body remains as a stream to be deserialized after routing.
    /// </summary>
    public interface IServiceRequest
    {
        /// <summary>
        /// The request path/address.
        /// For HTTP: "/pets/{petId}"
        /// For gRPC: "PetService.GetPetById"
        /// For AMQP: "pets.get"
        /// </summary>
        string Path { get; }

        /// <summary>
        /// The protocol-agnostic method/operation.
        /// For HTTP: "GET", "POST", "PUT", etc.
        /// For gRPC: "unary", "stream", etc.
        /// For AMQP: "request", "publish", etc.
        /// </summary>
        string Method { get; }

        /// <summary>
        /// Request headers/metadata.
        /// Protocol-agnostic representation of headers, context, metadata.
        /// </summary>
        IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// Query parameters extracted from the request.
        /// For HTTP: URL query string parameters
        /// For gRPC: Could be from metadata
        /// For AMQP: Could be from message properties
        /// </summary>
        IReadOnlyDictionary<string, string> QueryParameters { get; }

        /// <summary>
        /// Route parameters extracted from the path (populated after routing).
        /// Example: /pets/{petId} → RouteParameters["petId"] = "PET-123456"
        /// </summary>
        IDictionary<string, string> RouteParameters { get; }

        /// <summary>
        /// The request body as a stream.
        /// Remains undeserialized until after routing when we know the target type.
        /// Null for requests without a body (e.g., GET, DELETE).
        /// </summary>
        Stream? Body { get; }

        /// <summary>
        /// The content type of the request body.
        /// Used by deserializer to choose the appropriate serialization format.
        /// Example: "application/json", "application/protobuf", "application/xml"
        /// </summary>
        string? ContentType { get; }

        /// <summary>
        /// Correlation ID for distributed tracing.
        /// Extracted from headers or generated if not present.
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Extension properties for protocol-specific data.
        /// Allows transport adapters to pass additional context without modifying the interface.
        /// </summary>
        IReadOnlyDictionary<string, object> Properties { get; }
    }

    /// <summary>
    /// Builder for creating IServiceRequest instances.
    /// </summary>
    public interface IServiceRequestBuilder
    {
        IServiceRequestBuilder WithPath(string path);
        IServiceRequestBuilder WithMethod(string method);
        IServiceRequestBuilder WithHeader(string key, string value);
        IServiceRequestBuilder WithQueryParameter(string key, string value);
        IServiceRequestBuilder WithBody(Stream? body);
        IServiceRequestBuilder WithContentType(string? contentType);
        IServiceRequestBuilder WithCorrelationId(string correlationId);
        IServiceRequestBuilder WithProperty(string key, object value);
        IServiceRequest Build();
    }

    /// <summary>
    /// Default implementation of IServiceRequest.
    /// </summary>
    public sealed class ServiceRequest : IServiceRequest
    {
        public string Path { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
        public IReadOnlyDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> QueryParameters { get; init; } = new Dictionary<string, string>();
        public IDictionary<string, string> RouteParameters { get; init; } = new Dictionary<string, string>();
        public Stream? Body { get; init; }
        public string? ContentType { get; init; }
        public string CorrelationId { get; init; } = Guid.NewGuid().ToString();
        public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }
}
