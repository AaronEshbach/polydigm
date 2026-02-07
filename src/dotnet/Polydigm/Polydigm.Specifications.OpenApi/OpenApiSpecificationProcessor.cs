using Microsoft.OpenApi.Models;
using Polydigm.Specifications;

namespace Polydigm.Specifications.OpenApi
{
    /// <summary>
    /// Combined processor for OpenAPI specifications.
    /// Provides a single interface for parsing and extracting metadata.
    /// </summary>
    public sealed class OpenApiSpecificationProcessor : ISpecificationProcessor<OpenApiDocument>
    {
        private readonly OpenApiParser parser;
        private readonly OpenApiMetadataExtractor extractor;

        public OpenApiSpecificationProcessor(OpenApiSpecificationFormat? format = null)
        {
            parser = new OpenApiParser(format);
            extractor = new OpenApiMetadataExtractor();
        }

        public ISpecificationParser<OpenApiDocument> Parser => parser;
        public IMetadataExtractor<OpenApiDocument> Extractor => extractor;

        public async Task<MetadataExtractionResult> ProcessAsync(ISpecificationSource source, CancellationToken cancellationToken = default)
        {
            var spec = await Parser.ParseAsync(source, cancellationToken);
            return Extractor.ExtractAll(spec);
        }
    }
}
