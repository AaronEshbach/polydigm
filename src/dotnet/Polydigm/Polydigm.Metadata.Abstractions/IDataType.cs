namespace Polydigm.Metadata
{
    /// <summary>
    /// Represents a data type used by an application built on the Polydigm framework.
    /// Can be used to generate metadata in various formats such as OpenAPI, JSON Schema, etc.
    /// </summary>
    public interface IDataType
    {
        /// <summary>
        /// The name of the data type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The underlying CLR type of the raw data.
        /// </summary>
        TypeCode TypeCode { get; }

        /// <summary>
        /// The constraints applied to this data type.
        /// </summary>
        IReadOnlyList<IConstraint> Constraints { get; }

        /// <summary>
        /// The default value for this data type.
        /// </summary>
        object? DefaultValue { get; }

        /// <summary>
        /// The description of this data type.
        /// </summary>
        string? Description { get; }

        /// <summary>
        /// The format of this data type.
        /// </summary>
        string? Format { get; }
    }
}
