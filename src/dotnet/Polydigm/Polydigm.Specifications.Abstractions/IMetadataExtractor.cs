using Polydigm.Metadata;

namespace Polydigm.Specifications
{
    /// <summary>
    /// Extracts Polydigm metadata (IDataType, IModelMetadata) from a parsed specification.
    /// This is the bridge between specification formats and Polydigm's metadata model.
    /// </summary>
    /// <typeparam name="TSpec">The type representing the parsed specification structure.</typeparam>
    public interface IMetadataExtractor<TSpec>
    {
        /// <summary>
        /// Extracts all data types from a specification.
        /// </summary>
        /// <param name="specification">The parsed specification.</param>
        /// <returns>All data types defined in the specification.</returns>
        IEnumerable<IDataType> ExtractDataTypes(TSpec specification);

        /// <summary>
        /// Extracts all complex models from a specification.
        /// </summary>
        /// <param name="specification">The parsed specification.</param>
        /// <returns>All models defined in the specification.</returns>
        IEnumerable<IModelMetadata> ExtractModels(TSpec specification);

        /// <summary>
        /// Extracts all metadata (both data types and models) from a specification.
        /// </summary>
        /// <param name="specification">The parsed specification.</param>
        /// <returns>Complete metadata extraction result.</returns>
        MetadataExtractionResult ExtractAll(TSpec specification);
    }

    /// <summary>
    /// Result of extracting metadata from a specification.
    /// </summary>
    public sealed class MetadataExtractionResult
    {
        public IReadOnlyList<IDataType> DataTypes { get; init; } = Array.Empty<IDataType>();
        public IReadOnlyList<IModelMetadata> Models { get; init; } = Array.Empty<IModelMetadata>();
        public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Registers all extracted metadata with a metadata service.
        /// </summary>
        public void RegisterWith(IMetadataService metadataService)
        {
            foreach (var dataType in DataTypes)
            {
                metadataService.RegisterDataType(dataType);
            }

            foreach (var model in Models)
            {
                metadataService.RegisterModel(model);
            }
        }
    }

    /// <summary>
    /// Combined parser and extractor for a specific specification format.
    /// Provides a complete pipeline from specification source to metadata model.
    /// </summary>
    /// <typeparam name="TSpec">The type representing the parsed specification structure.</typeparam>
    public interface ISpecificationProcessor<TSpec>
    {
        /// <summary>
        /// The parser for this specification format.
        /// </summary>
        ISpecificationParser<TSpec> Parser { get; }

        /// <summary>
        /// The metadata extractor for this specification format.
        /// </summary>
        IMetadataExtractor<TSpec> Extractor { get; }

        /// <summary>
        /// Parses a specification and extracts metadata in one operation.
        /// </summary>
        /// <param name="source">The specification source.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Extracted metadata.</returns>
        Task<MetadataExtractionResult> ProcessAsync(ISpecificationSource source, CancellationToken cancellationToken = default);
    }
}
