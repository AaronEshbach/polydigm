namespace Polydigm.Metadata
{
    /// <summary>
    /// Represents a complete API/service with endpoints, models, and data types.
    /// Protocol-agnostic - can be implemented as HTTP REST, gRPC, SOAP, AMQP, GraphQL, etc.
    /// </summary>
    public interface IServiceMetadata
    {
        /// <summary>
        /// Name of the service/API.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Version of the service/API (e.g., "1.0.0", "v2").
        /// </summary>
        string? Version { get; }

        /// <summary>
        /// Description of what this service does.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// All endpoints/operations exposed by this service.
        /// </summary>
        IReadOnlyList<IEndpointMetadata> Endpoints { get; }

        /// <summary>
        /// All data types used by this service (validated primitives).
        /// </summary>
        IReadOnlyList<IDataType> DataTypes { get; }

        /// <summary>
        /// All complex models used by this service.
        /// </summary>
        IReadOnlyList<IModelMetadata> Models { get; }

        /// <summary>
        /// Protocol-specific extensions (e.g., base URL, server info).
        /// </summary>
        IReadOnlyDictionary<string, object>? Extensions { get; }
    }

    /// <summary>
    /// Concrete implementation of IServiceMetadata.
    /// </summary>
    public sealed class ServiceMetadata : IServiceMetadata
    {
        public string Name { get; init; } = string.Empty;
        public string? Version { get; init; }
        public string? Description { get; init; }
        public IReadOnlyList<IEndpointMetadata> Endpoints { get; init; } = Array.Empty<IEndpointMetadata>();
        public IReadOnlyList<IDataType> DataTypes { get; init; } = Array.Empty<IDataType>();
        public IReadOnlyList<IModelMetadata> Models { get; init; } = Array.Empty<IModelMetadata>();
        public IReadOnlyDictionary<string, object>? Extensions { get; init; }

        /// <summary>
        /// Looks up an endpoint by its canonical path.
        /// </summary>
        /// <param name="path">The endpoint path (e.g., "/pets/{petId}", "PetService.GetPetById")</param>
        /// <returns>The endpoint metadata, or null if not found.</returns>
        public IEndpointMetadata? GetEndpointByPath(string path)
        {
            return Endpoints.FirstOrDefault(e => e.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Looks up an endpoint by its name.
        /// </summary>
        /// <param name="name">The endpoint name (e.g., "GetPetById")</param>
        /// <returns>The endpoint metadata, or null if not found.</returns>
        public IEndpointMetadata? GetEndpointByName(string name)
        {
            return Endpoints.FirstOrDefault(e => e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
