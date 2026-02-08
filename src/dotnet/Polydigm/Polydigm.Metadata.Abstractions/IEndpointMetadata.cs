namespace Polydigm.Metadata
{
    /// <summary>
    /// Represents a callable operation/endpoint in an API, independent of protocol.
    /// Can be implemented as HTTP REST, gRPC, SOAP, AMQP, GraphQL, etc.
    /// </summary>
    public interface IEndpointMetadata
    {
        /// <summary>
        /// Unique identifier for this endpoint (e.g., "GetUserById", "CreateOrder").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Unique path/address for this endpoint within the service.
        /// This is the canonical, protocol-agnostic way to address the endpoint.
        /// Examples:
        /// - REST: "/pets/{petId}", "/orders/{id}/items"
        /// - gRPC: "PetService.GetPetById"
        /// - AMQP: "pets.get", "orders.create"
        /// - GraphQL: "Query.getPetById"
        /// This allows endpoints to reference each other without protocol-specific URLs.
        /// </summary>
        string Path { get; }

        /// <summary>
        /// Human-readable description of what this endpoint does.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// All input parameters accepted by this endpoint.
        /// </summary>
        IReadOnlyList<IInputParameter> Inputs { get; }

        /// <summary>
        /// All possible output responses from this endpoint.
        /// </summary>
        IReadOnlyList<IOutputResponse> Outputs { get; }

        /// <summary>
        /// Semantic characteristics of this endpoint (intent, safety, idempotency, etc.).
        /// </summary>
        IEndpointSemantics Semantics { get; }

        /// <summary>
        /// Protocol-specific extensions (e.g., HTTP path, gRPC service name, AMQP queue).
        /// </summary>
        IReadOnlyDictionary<string, object>? Extensions { get; }
    }

    /// <summary>
    /// Represents an input parameter to an endpoint.
    /// </summary>
    public interface IInputParameter
    {
        /// <summary>
        /// Name of the parameter (e.g., "userId", "orderData").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Data type of this parameter.
        /// </summary>
        IDataType DataType { get; }

        /// <summary>
        /// Whether this parameter is required.
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Hint about where this parameter comes from (path, query, body, etc.).
        /// Protocol-specific generators may interpret this differently.
        /// </summary>
        InputParameterKind Kind { get; }

        /// <summary>
        /// Description of this parameter.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Default value if not provided (only valid for optional parameters).
        /// </summary>
        object? DefaultValue { get; }
    }

    /// <summary>
    /// Hint about the semantic location/purpose of an input parameter.
    /// Protocol-specific generators interpret these hints appropriately for their protocol.
    /// </summary>
    public enum InputParameterKind
    {
        /// <summary>
        /// Part of the endpoint identifier/address (e.g., /users/{userId}).
        /// In HTTP: path parameter. In gRPC: part of request message.
        /// </summary>
        Path,

        /// <summary>
        /// Additional query/filter parameter (e.g., ?includeDeleted=true).
        /// In HTTP: query string. In gRPC: part of request message.
        /// </summary>
        Query,

        /// <summary>
        /// Metadata or header information (e.g., authorization, content-type).
        /// In HTTP: header. In gRPC: metadata. In AMQP: message properties.
        /// </summary>
        Header,

        /// <summary>
        /// Main request payload/body.
        /// In HTTP: request body. In gRPC: request message. In AMQP: message body.
        /// </summary>
        Body,

        /// <summary>
        /// From execution context (e.g., authenticated user, tenant ID).
        /// Typically injected by middleware/infrastructure, not from the caller.
        /// </summary>
        Context,

        /// <summary>
        /// No specific hint; let the generator decide based on conventions.
        /// </summary>
        Unspecified
    }

    /// <summary>
    /// Represents a possible output/response from an endpoint.
    /// </summary>
    public interface IOutputResponse
    {
        /// <summary>
        /// Name of this response (e.g., "Success", "NotFound", "ValidationError").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of when this response occurs.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// Data type of this response, if any (null for empty responses like 204 No Content).
        /// </summary>
        IDataType? DataType { get; }

        /// <summary>
        /// Category of this response (success, client error, server error).
        /// </summary>
        OutputKind Kind { get; }

        /// <summary>
        /// Protocol-specific extensions (e.g., HTTP status code).
        /// </summary>
        IReadOnlyDictionary<string, object>? Extensions { get; }
    }

    /// <summary>
    /// Category of an output response.
    /// </summary>
    public enum OutputKind
    {
        /// <summary>
        /// Successful response (HTTP 2xx, gRPC OK).
        /// </summary>
        Success,

        /// <summary>
        /// Client error - invalid request (HTTP 4xx, gRPC INVALID_ARGUMENT, etc.).
        /// </summary>
        ClientError,

        /// <summary>
        /// Server error - something went wrong on the server (HTTP 5xx, gRPC INTERNAL, etc.).
        /// </summary>
        ServerError,

        /// <summary>
        /// Redirect response (HTTP 3xx).
        /// </summary>
        Redirect,

        /// <summary>
        /// Informational response (HTTP 1xx).
        /// </summary>
        Informational
    }

    /// <summary>
    /// Semantic characteristics of an endpoint that describe its behavior.
    /// </summary>
    public interface IEndpointSemantics
    {
        /// <summary>
        /// The semantic intent of this operation.
        /// </summary>
        OperationIntent Intent { get; }

        /// <summary>
        /// Whether this operation is idempotent (can be called multiple times with the same result).
        /// Example: GET, PUT, DELETE are typically idempotent; POST is not.
        /// </summary>
        bool IsIdempotent { get; }

        /// <summary>
        /// Whether this operation is safe (read-only, no side effects).
        /// Example: GET is safe; POST, PUT, DELETE are not.
        /// </summary>
        bool IsSafe { get; }

        /// <summary>
        /// Whether this operation requires authentication.
        /// </summary>
        bool RequiresAuthentication { get; }

        /// <summary>
        /// Whether this operation is deprecated.
        /// </summary>
        bool IsDeprecated { get; }

        /// <summary>
        /// Tags/categories for organizing endpoints (e.g., "users", "orders").
        /// </summary>
        IReadOnlyList<string> Tags { get; }
    }

    /// <summary>
    /// The semantic intent of an operation, independent of protocol.
    /// </summary>
    public enum OperationIntent
    {
        /// <summary>
        /// Query/read operation - fetches data without side effects.
        /// Maps to: HTTP GET, gRPC unary call (GetX), GraphQL query.
        /// </summary>
        Query,

        /// <summary>
        /// Create operation - creates a new entity.
        /// Maps to: HTTP POST, gRPC CreateX, GraphQL mutation.
        /// </summary>
        Create,

        /// <summary>
        /// Update operation - modifies an existing entity.
        /// Maps to: HTTP PUT/PATCH, gRPC UpdateX, GraphQL mutation.
        /// </summary>
        Update,

        /// <summary>
        /// Delete operation - removes an entity.
        /// Maps to: HTTP DELETE, gRPC DeleteX, GraphQL mutation.
        /// </summary>
        Delete,

        /// <summary>
        /// Generic action/command - performs an operation that doesn't fit other categories.
        /// Maps to: HTTP POST, gRPC custom RPC, GraphQL mutation.
        /// </summary>
        Action,

        /// <summary>
        /// Event/notification - async message or event.
        /// Maps to: AsyncAPI publish/subscribe, AMQP message, gRPC stream.
        /// </summary>
        Event
    }
}
