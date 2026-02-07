using Polydigm.Metadata;

namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Main code generator for C# that produces validated types following Polydigm patterns.
    /// Generates:
    /// - Validated primitive wrappers (readonly record structs)
    /// - Validated models (sealed records)
    /// - DTOs (unvalidated boundary types)
    /// </summary>
    public sealed class CSharpCodeGenerator : ICodeGenerator
    {
        private readonly CSharpCodeGenerationTarget target;
        private readonly ValidatedPrimitiveGenerator primitiveGenerator;
        private readonly ValidatedModelGenerator modelGenerator;
        private readonly DtoGenerator dtoGenerator;

        public CSharpCodeGenerator(CSharpCodeGenerationTarget? target = null)
        {
            this.target = target ?? CSharpCodeGenerationTarget.Default;
            primitiveGenerator = new ValidatedPrimitiveGenerator();
            modelGenerator = new ValidatedModelGenerator();
            dtoGenerator = new DtoGenerator();
        }

        public ICodeGenerationTarget Target => target;

        public IGeneratedArtifact GenerateDataType(IDataType dataType, CodeGenerationOptions? options = null)
        {
            var generator = new ValidatedPrimitiveGenerator(options);
            var content = generator.Generate(dataType);

            var relativePath = BuildRelativePath(dataType.Name, options);

            return new CSharpGeneratedArtifact(
                name: $"{dataType.Name}.cs",
                content: content,
                relativePath: relativePath,
                target: target
            );
        }

        public IGeneratedArtifact GenerateModel(IModelMetadata model, CodeGenerationOptions? options = null)
        {
            var generator = new ValidatedModelGenerator(options);
            var content = generator.Generate(model);

            var relativePath = BuildRelativePath(model.Name, options);

            return new CSharpGeneratedArtifact(
                name: $"{model.Name}.cs",
                content: content,
                relativePath: relativePath,
                target: target
            );
        }

        public IEnumerable<IGeneratedArtifact> GenerateAll(GenerationInput input, CodeGenerationOptions? options = null)
        {
            var artifacts = new List<IGeneratedArtifact>();

            // Generate validated primitives
            foreach (var dataType in input.DataTypes)
            {
                artifacts.Add(GenerateDataType(dataType, options));
            }

            // Generate validated models
            foreach (var model in input.Models)
            {
                artifacts.Add(GenerateModel(model, options));
            }

            // Generate DTOs for models
            foreach (var model in input.Models)
            {
                artifacts.Add(GenerateDto(model, options));
            }

            return artifacts;
        }

        /// <summary>
        /// Generates a DTO type for a model.
        /// DTOs are unvalidated boundary types used for serialization.
        /// </summary>
        public IGeneratedArtifact GenerateDto(IModelMetadata model, CodeGenerationOptions? options = null)
        {
            var generator = new DtoGenerator(options);
            var content = generator.Generate(model);

            var relativePath = BuildRelativePath($"DTO/{model.Name}", options);

            return new CSharpGeneratedArtifact(
                name: $"{model.Name}.cs",
                content: content,
                relativePath: relativePath,
                target: target
            );
        }

        private string BuildRelativePath(string typeName, CodeGenerationOptions? options)
        {
            var outputDir = options?.OutputDirectory ?? "";
            var fileName = $"{typeName}.cs";

            if (string.IsNullOrWhiteSpace(outputDir))
            {
                return fileName;
            }

            return Path.Combine(outputDir, fileName);
        }
    }
}
