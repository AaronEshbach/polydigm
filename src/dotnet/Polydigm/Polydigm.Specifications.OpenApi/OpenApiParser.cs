using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Polydigm.Specifications;

namespace Polydigm.Specifications.OpenApi
{
    /// <summary>
    /// Parser for OpenAPI 3.x specifications using Microsoft.OpenApi.NET.
    /// </summary>
    public sealed class OpenApiParser : ISpecificationParser<OpenApiDocument>
    {
        private readonly OpenApiSpecificationFormat format;

        public OpenApiParser(OpenApiSpecificationFormat? format = null)
        {
            this.format = format ?? OpenApiSpecificationFormat.V3_0;
        }

        public ISpecificationFormat Format => format;

        public async Task<OpenApiDocument> ParseAsync(ISpecificationSource source, CancellationToken cancellationToken = default)
        {
            try
            {
                // Read the specification content as a stream
                using var stream = await source.ReadAsStreamAsync(cancellationToken);

                // Use Microsoft.OpenApi.Readers to parse
                var reader = new OpenApiStreamReader();
                var result = await reader.ReadAsync(stream, cancellationToken);

                // Check for errors
                if (result.OpenApiDiagnostic?.Errors?.Count > 0)
                {
                    var errors = result.OpenApiDiagnostic.Errors.Select(e => new SpecificationValidationError
                    {
                        Message = e.Message,
                        Path = e.Pointer
                    }).ToList();

                    throw new SpecificationParseException(
                        $"Failed to parse OpenAPI specification from {source.DisplayName}",
                        errors,
                        source
                    );
                }

                if (result.OpenApiDocument == null)
                {
                    throw new SpecificationParseException(
                        $"Failed to parse OpenAPI specification from {source.DisplayName}: No document returned",
                        source
                    );
                }

                return result.OpenApiDocument;
            }
            catch (SpecificationParseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SpecificationParseException(
                    $"Unexpected error parsing OpenAPI specification from {source.DisplayName}",
                    ex,
                    source
                );
            }
        }

        public async Task<SpecificationValidationResult> ValidateAsync(ISpecificationSource source, CancellationToken cancellationToken = default)
        {
            try
            {
                using var stream = await source.ReadAsStreamAsync(cancellationToken);
                var reader = new OpenApiStreamReader();
                var result = await reader.ReadAsync(stream, cancellationToken);

                if (result.OpenApiDiagnostic?.Errors?.Count > 0)
                {
                    var errors = result.OpenApiDiagnostic.Errors.Select(e => new SpecificationValidationError
                    {
                        Message = e.Message,
                        Path = e.Pointer
                    }).ToArray();

                    var warnings = result.OpenApiDiagnostic.Warnings?.Select(w => new SpecificationValidationWarning
                    {
                        Message = w.Message,
                        Path = w.Pointer
                    }).ToArray() ?? Array.Empty<SpecificationValidationWarning>();

                    return SpecificationValidationResult.Failure(errors);
                }

                var successWarnings = result.OpenApiDiagnostic?.Warnings?.Select(w => new SpecificationValidationWarning
                {
                    Message = w.Message,
                    Path = w.Pointer
                }).ToArray() ?? Array.Empty<SpecificationValidationWarning>();

                return new SpecificationValidationResult
                {
                    IsValid = true,
                    Warnings = successWarnings
                };
            }
            catch (Exception ex)
            {
                return SpecificationValidationResult.Failure(new SpecificationValidationError
                {
                    Message = $"Validation failed: {ex.Message}"
                });
            }
        }
    }
}
