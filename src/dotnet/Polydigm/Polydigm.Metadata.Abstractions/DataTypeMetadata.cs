using System.Text.RegularExpressions;

namespace Polydigm.Metadata
{
    /// <summary>
    /// Concrete implementation of IDataType for use by the metadata service.
    /// </summary>
    public sealed class DataTypeMetadata : IDataType
    {
        public string Name { get; init; } = string.Empty;
        public TypeCode TypeCode { get; init; }
        public IReadOnlyList<IConstraint> Constraints { get; init; } = Array.Empty<IConstraint>();
        public object? DefaultValue { get; init; }
        public string? Description { get; init; }
        public string? Format { get; init; }

        /// <summary>
        /// The runtime CLR type, if available.
        /// </summary>
        public Type? RuntimeType { get; init; }

        /// <summary>
        /// For validated types, the underlying primitive type that is being wrapped.
        /// E.g., for TestId, this would be typeof(string).
        /// </summary>
        public Type? UnderlyingType { get; init; }

        /// <summary>
        /// Indicates whether this is a validated wrapper type (vs a primitive).
        /// </summary>
        public bool IsValidated { get; init; }

        /// <summary>
        /// For validated types, the name of the static factory method (e.g., "TryCreate").
        /// </summary>
        public string? ValidationMethodName { get; init; }
    }

    /// <summary>
    /// Concrete implementation of IModelMetadata for use by the metadata service.
    /// </summary>
    public sealed class ModelMetadata : IModelMetadata
    {
        public string Name { get; init; } = string.Empty;
        public string? Namespace { get; init; }
        public string FullName => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
        public Type? RuntimeType { get; init; }
        public string? Description { get; init; }
        public IReadOnlyList<IFieldMetadata> Fields { get; init; } = Array.Empty<IFieldMetadata>();
        public ModelKind Kind { get; init; }
        public string? BaseType { get; init; }
        public IReadOnlyDictionary<string, object>? Extensions { get; init; }
    }

    /// <summary>
    /// Concrete implementation of IFieldMetadata for use by the metadata service.
    /// </summary>
    public sealed class FieldMetadata : IFieldMetadata
    {
        public string Name { get; init; } = string.Empty;
        public IDataType DataType { get; init; } = null!;
        public bool IsRequired { get; init; }
        public bool IsNullable { get; init; }
        public bool IsReadOnly { get; init; }
        public string? Description { get; init; }
        public object? DefaultValue { get; init; }
        public IReadOnlyList<object>? Examples { get; init; }
        public bool IsCollection { get; init; }
        public IDataType? CollectionElementType { get; init; }
    }

    // ============ Constraint Implementations ============

    /// <summary>
    /// Concrete implementation of IRequiredConstraint.
    /// </summary>
    public sealed class RequiredConstraint : IRequiredConstraint
    {
        public bool IsRequired { get; init; } = true;
    }

    /// <summary>
    /// Concrete implementation of IPatternConstraint.
    /// </summary>
    public sealed class PatternConstraint : IPatternConstraint
    {
        public Regex Pattern { get; init; } = null!;

        public PatternConstraint(Regex pattern)
        {
            Pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        }

        public PatternConstraint(string pattern, RegexOptions options = RegexOptions.None)
        {
            Pattern = new Regex(pattern, options);
        }
    }

    /// <summary>
    /// Concrete implementation of IMinimumConstraint.
    /// </summary>
    public sealed class MinimumConstraint : IMinimumConstraint
    {
        public IComparable Minimum { get; init; }
        public BoundaryMode BoundaryMode { get; init; }

        public MinimumConstraint(IComparable minimum, BoundaryMode boundaryMode = BoundaryMode.Inclusive)
        {
            Minimum = minimum ?? throw new ArgumentNullException(nameof(minimum));
            BoundaryMode = boundaryMode;
        }
    }

    /// <summary>
    /// Concrete implementation of IMaximumConstraint.
    /// </summary>
    public sealed class MaximumConstraint : IMaximumConstraint
    {
        public IComparable Maximum { get; init; }
        public BoundaryMode BoundaryMode { get; init; }

        public MaximumConstraint(IComparable maximum, BoundaryMode boundaryMode = BoundaryMode.Inclusive)
        {
            Maximum = maximum ?? throw new ArgumentNullException(nameof(maximum));
            BoundaryMode = boundaryMode;
        }
    }

    /// <summary>
    /// Concrete implementation of IMinimumLengthConstraint.
    /// </summary>
    public sealed class MinimumLengthConstraint : IMinimumLengthConstraint
    {
        public long MinimumLength { get; init; }
        public BoundaryMode BoundaryMode { get; init; }

        public MinimumLengthConstraint(long minimumLength, BoundaryMode boundaryMode = BoundaryMode.Inclusive)
        {
            if (minimumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength), "Minimum length cannot be negative.");
            MinimumLength = minimumLength;
            BoundaryMode = boundaryMode;
        }
    }

    /// <summary>
    /// Concrete implementation of IMaximumLengthConstraint.
    /// </summary>
    public sealed class MaximumLengthConstraint : IMaximumLengthConstraint
    {
        public long MaximumLength { get; init; }
        public BoundaryMode BoundaryMode { get; init; }

        public MaximumLengthConstraint(long maximumLength, BoundaryMode boundaryMode = BoundaryMode.Inclusive)
        {
            if (maximumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maximumLength), "Maximum length cannot be negative.");
            MaximumLength = maximumLength;
            BoundaryMode = boundaryMode;
        }
    }
}
