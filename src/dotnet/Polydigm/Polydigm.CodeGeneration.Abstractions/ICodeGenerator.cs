using Polydigm.Metadata;

namespace Polydigm.CodeGeneration
{
    /// <summary>
    /// Generates code in a specific language from Polydigm metadata.
    /// </summary>
    public interface ICodeGenerator
    {
        /// <summary>
        /// The target language/platform for this code generator.
        /// </summary>
        ICodeGenerationTarget Target { get; }

        /// <summary>
        /// Generates code for a validated data type.
        /// </summary>
        /// <param name="dataType">The data type metadata.</param>
        /// <param name="options">Generation options.</param>
        /// <returns>Generated code artifact.</returns>
        IGeneratedArtifact GenerateDataType(IDataType dataType, CodeGenerationOptions? options = null);

        /// <summary>
        /// Generates code for a complex model.
        /// </summary>
        /// <param name="model">The model metadata.</param>
        /// <param name="options">Generation options.</param>
        /// <returns>Generated code artifact.</returns>
        IGeneratedArtifact GenerateModel(IModelMetadata model, CodeGenerationOptions? options = null);

        /// <summary>
        /// Generates code for multiple types at once.
        /// </summary>
        /// <param name="input">The metadata to generate code for.</param>
        /// <param name="options">Generation options.</param>
        /// <returns>Collection of generated code artifacts.</returns>
        IEnumerable<IGeneratedArtifact> GenerateAll(GenerationInput input, CodeGenerationOptions? options = null);
    }

    /// <summary>
    /// Describes a code generation target (language, framework, patterns).
    /// </summary>
    public interface ICodeGenerationTarget
    {
        /// <summary>
        /// The target language (e.g., "C#", "TypeScript", "Python").
        /// </summary>
        string Language { get; }

        /// <summary>
        /// The language version (e.g., "10.0", "5.0", "3.11").
        /// </summary>
        string? LanguageVersion { get; }

        /// <summary>
        /// The target framework or platform (e.g., ".NET 10", "ES2022", "3.11").
        /// </summary>
        string? TargetFramework { get; }

        /// <summary>
        /// Full display name (e.g., "C# 10.0 (.NET 10)").
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// File extension for generated files (e.g., ".cs", ".ts", ".py").
        /// </summary>
        string FileExtension { get; }
    }

    /// <summary>
    /// Represents a generated code artifact (file, class, module, etc.)
    /// </summary>
    public interface IGeneratedArtifact
    {
        /// <summary>
        /// The type of artifact (File, Class, Interface, etc.)
        /// </summary>
        ArtifactType Type { get; }

        /// <summary>
        /// The name of the artifact (e.g., "TestId.cs", "TestId").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The relative path where this artifact should be placed (e.g., "Models/TestId.cs").
        /// </summary>
        string RelativePath { get; }

        /// <summary>
        /// The generated source code content.
        /// </summary>
        string Content { get; }

        /// <summary>
        /// The target language for this artifact.
        /// </summary>
        ICodeGenerationTarget Target { get; }

        /// <summary>
        /// Writes the artifact to disk at the specified base directory.
        /// </summary>
        /// <param name="baseDirectory">The base directory to write to.</param>
        void WriteToDisk(string baseDirectory);

        /// <summary>
        /// Writes the artifact to disk asynchronously.
        /// </summary>
        Task WriteToDiskAsync(string baseDirectory, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Options for code generation.
    /// </summary>
    public sealed class CodeGenerationOptions
    {
        /// <summary>
        /// Namespace to generate code into (language-specific: namespace in C#, module in Python, etc.)
        /// </summary>
        public string? Namespace { get; init; }

        /// <summary>
        /// Base output directory for generated files.
        /// </summary>
        public string? OutputDirectory { get; init; }

        /// <summary>
        /// Whether to include XML documentation comments.
        /// </summary>
        public bool IncludeDocumentation { get; init; } = true;

        /// <summary>
        /// Whether to include validation attributes in generated code.
        /// </summary>
        public bool IncludeValidationAttributes { get; init; } = true;

        /// <summary>
        /// Whether to generate partial classes (C# specific).
        /// </summary>
        public bool GeneratePartialClasses { get; init; } = false;

        /// <summary>
        /// Whether to use nullable reference types (C# 8+ specific).
        /// </summary>
        public bool UseNullableReferenceTypes { get; init; } = true;

        /// <summary>
        /// Whether to use records instead of classes where applicable (C# 9+ specific).
        /// </summary>
        public bool UseRecords { get; init; } = true;

        /// <summary>
        /// Whether to use readonly structs for validated types (C# specific).
        /// </summary>
        public bool UseReadonlyStructs { get; init; } = true;

        /// <summary>
        /// Custom file header template (copyright notice, auto-generated warning, etc.)
        /// </summary>
        public string? FileHeaderTemplate { get; init; }

        /// <summary>
        /// Additional imports/usings to include in generated files.
        /// </summary>
        public IReadOnlyList<string>? AdditionalImports { get; init; }

        /// <summary>
        /// Extension data for language-specific options.
        /// </summary>
        public Dictionary<string, object>? ExtensionData { get; init; }

        /// <summary>
        /// Default options with sensible defaults.
        /// </summary>
        public static CodeGenerationOptions Default => new();
    }

    /// <summary>
    /// Types of generated artifacts.
    /// </summary>
    public enum ArtifactType
    {
        /// <summary>
        /// A complete source file.
        /// </summary>
        File,

        /// <summary>
        /// A class definition.
        /// </summary>
        Class,

        /// <summary>
        /// A struct definition.
        /// </summary>
        Struct,

        /// <summary>
        /// An interface definition.
        /// </summary>
        Interface,

        /// <summary>
        /// An enum definition.
        /// </summary>
        Enum,

        /// <summary>
        /// A module or namespace.
        /// </summary>
        Module
    }

    /// <summary>
    /// Exception thrown when code generation fails.
    /// </summary>
    public class CodeGenerationException : Exception
    {
        public ICodeGenerationTarget? Target { get; }
        public string? TypeName { get; }

        public CodeGenerationException(string message)
            : base(message)
        {
        }

        public CodeGenerationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public CodeGenerationException(string message, ICodeGenerationTarget target, string? typeName = null)
            : base(message)
        {
            Target = target;
            TypeName = typeName;
        }
    }
}
