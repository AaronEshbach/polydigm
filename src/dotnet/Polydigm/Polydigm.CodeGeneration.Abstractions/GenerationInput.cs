using Polydigm.Metadata;

namespace Polydigm.CodeGeneration
{
    /// <summary>
    /// Input for code generation operations.
    /// </summary>
    public sealed class GenerationInput
    {
        public IReadOnlyList<IDataType> DataTypes { get; init; } = Array.Empty<IDataType>();
        public IReadOnlyList<IModelMetadata> Models { get; init; } = Array.Empty<IModelMetadata>();

        public static GenerationInput FromDataTypes(params IDataType[] dataTypes)
            => new() { DataTypes = dataTypes };

        public static GenerationInput FromModels(params IModelMetadata[] models)
            => new() { Models = models };

        public static GenerationInput From(IEnumerable<IDataType> dataTypes, IEnumerable<IModelMetadata> models)
            => new() { DataTypes = dataTypes.ToList(), Models = models.ToList() };
    }
}
