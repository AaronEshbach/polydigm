namespace Polydigm.Execution
{
    /// <summary>
    /// Handles serialization and deserialization of data between transport layer and application layer.
    /// Different implementations for JSON, XML, Protocol Buffers, MessagePack, etc.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Deserializes input from a stream into the target type.
        /// </summary>
        /// <param name="input">The input stream containing serialized data.</param>
        /// <param name="targetType">The type to deserialize into (typically a DTO).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized object.</returns>
        Task<object?> DeserializeAsync(Stream input, Type targetType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes input from a byte array into the target type.
        /// </summary>
        /// <param name="input">The byte array containing serialized data.</param>
        /// <param name="targetType">The type to deserialize into (typically a DTO).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized object.</returns>
        Task<object?> DeserializeAsync(byte[] input, Type targetType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deserializes input from a string into the target type.
        /// </summary>
        /// <param name="input">The string containing serialized data.</param>
        /// <param name="targetType">The type to deserialize into (typically a DTO).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The deserialized object.</returns>
        Task<object?> DeserializeAsync(string input, Type targetType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes an object to a stream.
        /// </summary>
        /// <param name="output">The output stream to write to.</param>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SerializeAsync(Stream output, object? value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes an object to a byte array.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The serialized byte array.</returns>
        Task<byte[]> SerializeToArrayAsync(object? value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Serializes an object to a string.
        /// </summary>
        /// <param name="value">The object to serialize.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The serialized string.</returns>
        Task<string> SerializeToStringAsync(object? value, CancellationToken cancellationToken = default);

        /// <summary>
        /// The content type this serializer produces (e.g., "application/json", "application/protobuf").
        /// </summary>
        string ContentType { get; }
    }
}
