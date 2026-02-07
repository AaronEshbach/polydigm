namespace Polydigm.Metadata
{
    /// <summary>
    /// Provides access to metadata for types used in Polydigm applications.
    /// Enables runtime introspection and generation of specifications (OpenAPI, JSON Schema, etc.)
    /// </summary>
    public interface IMetadataService
    {
        // ============ Data Type Metadata (Validated Types & Primitives) ============

        /// <summary>
        /// Gets metadata for a validated data type or primitive by its runtime Type.
        /// </summary>
        /// <param name="type">The CLR type to get metadata for.</param>
        /// <returns>Metadata describing the type's constraints and properties.</returns>
        IDataType GetDataType(Type type);

        /// <summary>
        /// Gets metadata for a validated data type or primitive by its runtime Type.
        /// </summary>
        /// <typeparam name="T">The CLR type to get metadata for.</typeparam>
        /// <returns>Metadata describing the type's constraints and properties.</returns>
        IDataType GetDataType<T>();

        /// <summary>
        /// Gets metadata for a data type by its name (e.g., "TestId").
        /// Useful when generating code from specifications.
        /// </summary>
        /// <param name="typeName">The name of the type.</param>
        /// <returns>Metadata for the type, or null if not found.</returns>
        IDataType? GetDataType(string typeName);

        /// <summary>
        /// Gets all registered data types.
        /// </summary>
        /// <returns>All data types known to the metadata service.</returns>
        IEnumerable<IDataType> GetAllDataTypes();

        /// <summary>
        /// Gets only validated types (excludes primitives like string, int, etc.)
        /// </summary>
        /// <returns>All validated wrapper types.</returns>
        IEnumerable<IDataType> GetValidatedTypes();

        /// <summary>
        /// Registers metadata for a data type. Used by code generators or for manual registration.
        /// </summary>
        /// <param name="dataType">The data type metadata to register.</param>
        void RegisterDataType(IDataType dataType);

        // ============ Model Metadata (Complex Types) ============

        /// <summary>
        /// Gets metadata for a complex model type (classes/records with multiple fields).
        /// </summary>
        /// <param name="type">The CLR type to get model metadata for.</param>
        /// <returns>Metadata describing the model's structure, or null if type is not a model.</returns>
        IModelMetadata? GetModelMetadata(Type type);

        /// <summary>
        /// Gets metadata for a complex model type (classes/records with multiple fields).
        /// </summary>
        /// <typeparam name="T">The CLR type to get model metadata for.</typeparam>
        /// <returns>Metadata describing the model's structure, or null if type is not a model.</returns>
        IModelMetadata? GetModelMetadata<T>();

        /// <summary>
        /// Gets model metadata by name (e.g., "User", "Order").
        /// </summary>
        /// <param name="modelName">The name of the model.</param>
        /// <returns>Metadata for the model, or null if not found.</returns>
        IModelMetadata? GetModelMetadata(string modelName);

        /// <summary>
        /// Gets all registered model types.
        /// </summary>
        /// <returns>All models known to the metadata service.</returns>
        IEnumerable<IModelMetadata> GetAllModels();

        /// <summary>
        /// Registers metadata for a complex model. Used by code generators or for manual registration.
        /// </summary>
        /// <param name="model">The model metadata to register.</param>
        void RegisterModel(IModelMetadata model);

        // ============ Reflection-Based Discovery ============

        /// <summary>
        /// Scans an assembly for types marked with [Validated] or other metadata attributes
        /// and automatically registers them.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        void ScanAssembly(System.Reflection.Assembly assembly);

        /// <summary>
        /// Attempts to extract metadata from a type via reflection, even if not pre-registered.
        /// Returns null if the type doesn't conform to Polydigm patterns.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>Extracted data type metadata, or null if not applicable.</returns>
        IDataType? TryExtractDataType(Type type);

        /// <summary>
        /// Attempts to extract model metadata from a type via reflection.
        /// Returns null if the type doesn't conform to Polydigm patterns.
        /// </summary>
        /// <param name="type">The type to analyze.</param>
        /// <returns>Extracted model metadata, or null if not applicable.</returns>
        IModelMetadata? TryExtractModelMetadata(Type type);
    }
}
