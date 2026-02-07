namespace Polydigm.Specifications
{
    /// <summary>
    /// Represents an abstract parser for any specification format (OpenAPI, JSON Schema, AsyncAPI, etc.)
    /// Parsers convert specification files into Polydigm's metadata model.
    /// </summary>
    /// <typeparam name="TSpec">The type representing the parsed specification structure.</typeparam>
    public interface ISpecificationParser<TSpec>
    {
        /// <summary>
        /// Gets the specification format this parser handles.
        /// </summary>
        ISpecificationFormat Format { get; }

        /// <summary>
        /// Parses a specification from a source and returns the parsed structure.
        /// </summary>
        /// <param name="source">The source containing the specification (file, URL, string, etc.)</param>
        /// <returns>The parsed specification structure.</returns>
        /// <exception cref="SpecificationParseException">Thrown when parsing fails.</exception>
        Task<TSpec> ParseAsync(ISpecificationSource source, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a specification source without fully parsing it.
        /// </summary>
        /// <param name="source">The source to validate.</param>
        /// <returns>True if the specification is valid, false otherwise.</returns>
        Task<SpecificationValidationResult> ValidateAsync(ISpecificationSource source, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Describes a specification format (OpenAPI 3.0, JSON Schema Draft 7, etc.)
    /// </summary>
    public interface ISpecificationFormat
    {
        /// <summary>
        /// The name of the specification format (e.g., "OpenAPI", "JSON Schema", "AsyncAPI").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The version of the specification format (e.g., "3.0.0", "3.1.0", "Draft 7").
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Full display name (e.g., "OpenAPI 3.0.3", "JSON Schema Draft 7").
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Supported file extensions (e.g., [".yaml", ".yml", ".json"]).
        /// </summary>
        IReadOnlyList<string> SupportedExtensions { get; }

        /// <summary>
        /// MIME types associated with this format (e.g., ["application/json", "application/yaml"]).
        /// </summary>
        IReadOnlyList<string> MimeTypes { get; }
    }

    /// <summary>
    /// Represents a source containing a specification (file, URL, string content, etc.)
    /// </summary>
    public interface ISpecificationSource
    {
        /// <summary>
        /// The type of source (File, Url, String, Stream, etc.)
        /// </summary>
        SpecificationSourceType SourceType { get; }

        /// <summary>
        /// A descriptive name or path for this source (for error messages).
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Reads the specification content as a string.
        /// </summary>
        Task<string> ReadAsStringAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the specification content as a stream.
        /// </summary>
        Task<Stream> ReadAsStreamAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of validating a specification.
    /// </summary>
    public sealed class SpecificationValidationResult
    {
        public bool IsValid { get; init; }
        public IReadOnlyList<SpecificationValidationError> Errors { get; init; } = Array.Empty<SpecificationValidationError>();
        public IReadOnlyList<SpecificationValidationWarning> Warnings { get; init; } = Array.Empty<SpecificationValidationWarning>();

        public static SpecificationValidationResult Success() => new() { IsValid = true };

        public static SpecificationValidationResult Failure(params SpecificationValidationError[] errors)
            => new() { IsValid = false, Errors = errors };
    }

    /// <summary>
    /// Represents a validation error in a specification.
    /// </summary>
    public sealed class SpecificationValidationError
    {
        public string Message { get; init; } = string.Empty;
        public string? Path { get; init; }
        public int? LineNumber { get; init; }
        public int? ColumnNumber { get; init; }
        public string? ErrorCode { get; init; }
    }

    /// <summary>
    /// Represents a validation warning in a specification.
    /// </summary>
    public sealed class SpecificationValidationWarning
    {
        public string Message { get; init; } = string.Empty;
        public string? Path { get; init; }
        public string? WarningCode { get; init; }
    }

    /// <summary>
    /// Types of specification sources.
    /// </summary>
    public enum SpecificationSourceType
    {
        File,
        Url,
        String,
        Stream
    }

    /// <summary>
    /// Exception thrown when specification parsing fails.
    /// </summary>
    public class SpecificationParseException : Exception
    {
        public ISpecificationSource? SpecificationSource { get; }
        public IReadOnlyList<SpecificationValidationError>? Errors { get; }

        public SpecificationParseException(string message, ISpecificationSource? source = null)
            : base(message)
        {
            SpecificationSource = source;
        }

        public SpecificationParseException(string message, IReadOnlyList<SpecificationValidationError> errors, ISpecificationSource? source = null)
            : base(message)
        {
            Errors = errors;
            SpecificationSource = source;
        }

        public SpecificationParseException(string message, Exception innerException, ISpecificationSource? source = null)
            : base(message, innerException)
        {
            SpecificationSource = source;
        }
    }
}
