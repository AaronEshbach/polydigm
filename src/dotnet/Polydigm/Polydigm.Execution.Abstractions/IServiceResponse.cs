namespace Polydigm.Execution
{
    /// <summary>
    /// Represents a protocol-agnostic service response.
    /// Pipeline produces this model, which transport adapters convert to wire format.
    /// The body remains as a stream after serialization.
    /// </summary>
    public interface IServiceResponse
    {
        /// <summary>
        /// Response status code.
        /// Protocol-agnostic representation:
        /// - HTTP: 200, 404, 500, etc.
        /// - gRPC: Maps to status codes (OK=200, NOT_FOUND=404, etc.)
        /// - AMQP: Success/failure indication
        /// </summary>
        int StatusCode { get; set; }

        /// <summary>
        /// Response headers/metadata.
        /// </summary>
        IDictionary<string, string> Headers { get; }

        /// <summary>
        /// The response body as a stream.
        /// Set by the serialization component after converting the result object.
        /// Null for empty responses (e.g., 204 No Content, DELETE success).
        /// </summary>
        Stream? Body { get; set; }

        /// <summary>
        /// The content type of the response body.
        /// Set by the serializer based on negotiation or default format.
        /// Example: "application/json", "application/protobuf", "application/xml"
        /// </summary>
        string? ContentType { get; set; }

        /// <summary>
        /// Correlation ID for distributed tracing.
        /// Should match the request correlation ID.
        /// </summary>
        string CorrelationId { get; set; }

        /// <summary>
        /// Extension properties for protocol-specific data.
        /// </summary>
        IDictionary<string, object> Properties { get; }
    }

    /// <summary>
    /// Builder for creating IServiceResponse instances.
    /// </summary>
    public interface IServiceResponseBuilder
    {
        IServiceResponseBuilder WithStatusCode(int statusCode);
        IServiceResponseBuilder WithHeader(string key, string value);
        IServiceResponseBuilder WithBody(Stream? body);
        IServiceResponseBuilder WithContentType(string? contentType);
        IServiceResponseBuilder WithCorrelationId(string correlationId);
        IServiceResponseBuilder WithProperty(string key, object value);
        IServiceResponse Build();
    }

    /// <summary>
    /// Default implementation of IServiceResponse.
    /// </summary>
    public sealed class ServiceResponse : IServiceResponse
    {
        public int StatusCode { get; set; } = 200;
        public IDictionary<string, string> Headers { get; init; } = new Dictionary<string, string>();
        public Stream? Body { get; set; }
        public string? ContentType { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public IDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
    }
}
