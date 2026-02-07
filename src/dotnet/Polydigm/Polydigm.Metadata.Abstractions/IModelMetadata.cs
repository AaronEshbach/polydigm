namespace Polydigm.Metadata
{
    /// <summary>
    /// Represents metadata for a complex model type (classes/records with multiple fields).
    /// Examples: User, Order, CreateUserRequest, etc.
    /// </summary>
    public interface IModelMetadata
    {
        /// <summary>
        /// The name of the model (e.g., "User", "Order").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The namespace containing the model (e.g., "MyApp.Models").
        /// </summary>
        string? Namespace { get; }

        /// <summary>
        /// The fully qualified name (e.g., "MyApp.Models.User").
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// The CLR runtime type, if available. Null for models defined in specs but not yet generated.
        /// </summary>
        Type? RuntimeType { get; }

        /// <summary>
        /// A description of what this model represents.
        /// Can be extracted from XML docs or specifications.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// The fields/properties that make up this model.
        /// </summary>
        IReadOnlyList<IFieldMetadata> Fields { get; }

        /// <summary>
        /// Indicates whether this is a request/command model (input) or response/query model (output).
        /// Useful for generating appropriate OpenAPI schemas.
        /// </summary>
        ModelKind Kind { get; }

        /// <summary>
        /// Base type or interface that this model implements, if any.
        /// </summary>
        string? BaseType { get; }

        /// <summary>
        /// Additional metadata for extension (e.g., API endpoint info, validation rules at model level).
        /// </summary>
        IReadOnlyDictionary<string, object>? Extensions { get; }
    }

    /// <summary>
    /// Represents metadata for a field or property within a model.
    /// </summary>
    public interface IFieldMetadata
    {
        /// <summary>
        /// The name of the field/property.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The data type of this field (could be primitive, validated, or another complex model).
        /// </summary>
        IDataType DataType { get; }

        /// <summary>
        /// Whether this field is required (non-nullable).
        /// </summary>
        bool IsRequired { get; }

        /// <summary>
        /// Whether this field is nullable.
        /// </summary>
        bool IsNullable { get; }

        /// <summary>
        /// Whether this field is read-only (has no setter).
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// A description of what this field represents.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// The default value for this field, if any.
        /// </summary>
        object? DefaultValue { get; }

        /// <summary>
        /// Example value(s) for documentation purposes.
        /// </summary>
        IReadOnlyList<object>? Examples { get; }

        /// <summary>
        /// Indicates if this field is a collection (array, list, etc.)
        /// </summary>
        bool IsCollection { get; }

        /// <summary>
        /// If IsCollection is true, the type of elements in the collection.
        /// </summary>
        IDataType? CollectionElementType { get; }
    }

    /// <summary>
    /// Categorizes the purpose of a model.
    /// </summary>
    public enum ModelKind
    {
        /// <summary>
        /// General-purpose model (domain entity, value object, etc.)
        /// </summary>
        Entity = 0,

        /// <summary>
        /// Request/command model (input to an operation)
        /// </summary>
        Request = 1,

        /// <summary>
        /// Response/result model (output from an operation)
        /// </summary>
        Response = 2,

        /// <summary>
        /// Data transfer object (DTO)
        /// </summary>
        Dto = 3,

        /// <summary>
        /// Event model (for pub/sub, notifications, etc.)
        /// </summary>
        Event = 4
    }
}
