namespace Polydigm.Metadata
{
    /// <summary>
    /// Concrete implementation of IEndpointMetadata.
    /// </summary>
    public sealed class EndpointMetadata : IEndpointMetadata
    {
        public string Name { get; init; } = string.Empty;
        public string Path { get; init; } = string.Empty;
        public string? Description { get; init; }
        public IReadOnlyList<IInputParameter> Inputs { get; init; } = Array.Empty<IInputParameter>();
        public IReadOnlyList<IOutputResponse> Outputs { get; init; } = Array.Empty<IOutputResponse>();
        public IEndpointSemantics Semantics { get; init; } = new EndpointSemantics();
        public IReadOnlyDictionary<string, object>? Extensions { get; init; }
    }

    /// <summary>
    /// Concrete implementation of IInputParameter.
    /// </summary>
    public sealed class InputParameter : IInputParameter
    {
        public string Name { get; init; } = string.Empty;
        public IDataType DataType { get; init; } = null!;
        public bool IsRequired { get; init; }
        public InputParameterKind Kind { get; init; }
        public string? Description { get; init; }
        public object? DefaultValue { get; init; }
    }

    /// <summary>
    /// Concrete implementation of IOutputResponse.
    /// </summary>
    public sealed class OutputResponse : IOutputResponse
    {
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
        public IDataType? DataType { get; init; }
        public OutputKind Kind { get; init; }
        public IReadOnlyDictionary<string, object>? Extensions { get; init; }
    }

    /// <summary>
    /// Concrete implementation of IEndpointSemantics.
    /// </summary>
    public sealed class EndpointSemantics : IEndpointSemantics
    {
        public OperationIntent Intent { get; init; }
        public bool IsIdempotent { get; init; }
        public bool IsSafe { get; init; }
        public bool RequiresAuthentication { get; init; }
        public bool IsDeprecated { get; init; }
        public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    }
}
