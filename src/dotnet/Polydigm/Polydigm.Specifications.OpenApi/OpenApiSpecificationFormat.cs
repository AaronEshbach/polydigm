using Polydigm.Specifications;

namespace Polydigm.Specifications.OpenApi
{
    /// <summary>
    /// Specification format descriptor for OpenAPI 3.x.
    /// </summary>
    public sealed class OpenApiSpecificationFormat : ISpecificationFormat
    {
        public OpenApiSpecificationFormat(string version = "3.0")
        {
            Version = version;
        }

        public string Name => "OpenAPI";
        public string Version { get; }
        public string FullName => $"{Name} {Version}";

        public IReadOnlyList<string> SupportedExtensions { get; } = new[]
        {
            ".yaml",
            ".yml",
            ".json"
        };

        public IReadOnlyList<string> MimeTypes { get; } = new[]
        {
            "application/json",
            "application/yaml",
            "application/x-yaml",
            "text/yaml",
            "text/x-yaml"
        };

        /// <summary>
        /// OpenAPI 3.0.x format.
        /// </summary>
        public static OpenApiSpecificationFormat V3_0 => new("3.0");

        /// <summary>
        /// OpenAPI 3.1.x format.
        /// </summary>
        public static OpenApiSpecificationFormat V3_1 => new("3.1");
    }
}
