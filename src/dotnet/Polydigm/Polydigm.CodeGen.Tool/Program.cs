using System.CommandLine;
using Polydigm.CodeGeneration;
using Polydigm.CodeGeneration.CSharp;
using Polydigm.Specifications;
using Polydigm.Specifications.OpenApi;

namespace Polydigm.CodeGen.Tool;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Polydigm code generation tool - Generate validated models from API specifications");

        // Generate command
        var generateCommand = new Command("generate", "Generate code from an API specification");

        var fromOption = new Option<FileInfo>(
            aliases: new[] { "--from", "-f" },
            description: "Path to the API specification file (OpenAPI YAML/JSON)")
        {
            IsRequired = true
        };
        fromOption.AddValidator(result =>
        {
            var file = result.GetValueForOption(fromOption);
            if (file != null && !file.Exists)
            {
                result.ErrorMessage = $"File not found: {file.FullName}";
            }
        });

        var toOption = new Option<DirectoryInfo>(
            aliases: new[] { "--to", "-t" },
            description: "Output directory for generated code",
            getDefaultValue: () => new DirectoryInfo("Generated"))
        {
            IsRequired = false
        };

        var namespaceOption = new Option<string>(
            aliases: new[] { "--namespace", "-n" },
            description: "Namespace for generated code",
            getDefaultValue: () => "Generated.Models")
        {
            IsRequired = false
        };

        var languageOption = new Option<string>(
            aliases: new[] { "--language", "-l" },
            description: "Target language for code generation",
            getDefaultValue: () => "csharp")
        {
            IsRequired = false
        };

        generateCommand.AddOption(fromOption);
        generateCommand.AddOption(toOption);
        generateCommand.AddOption(namespaceOption);
        generateCommand.AddOption(languageOption);

        generateCommand.SetHandler(async (file, outputDir, ns, language) =>
        {
            await GenerateCodeAsync(file!, outputDir!, ns!, language!);
        }, fromOption, toOption, namespaceOption, languageOption);

        rootCommand.AddCommand(generateCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task GenerateCodeAsync(FileInfo specFile, DirectoryInfo outputDir, string namespaceName, string language)
    {
        try
        {
            Console.WriteLine($"🚀 Polydigm Code Generator");
            Console.WriteLine($"📄 Reading specification: {specFile.FullName}");
            Console.WriteLine();

            // Only C# is supported for now
            if (language.ToLowerInvariant() != "csharp")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: Language '{language}' is not supported. Only 'csharp' is currently available.");
                Console.ResetColor();
                return;
            }

            // Parse OpenAPI specification
            var processor = new OpenApiSpecificationProcessor();
            var source = SpecificationSource.FromFile(specFile.FullName);

            Console.WriteLine("🔍 Parsing OpenAPI specification...");
            var metadata = await processor.ProcessAsync(source);

            Console.WriteLine($"✅ Found {metadata.DataTypes.Count} data types");
            Console.WriteLine($"✅ Found {metadata.Models.Count} models");

            if (metadata.Warnings.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"⚠️  {metadata.Warnings.Count} warnings:");
                foreach (var warning in metadata.Warnings)
                {
                    Console.WriteLine($"   - {warning}");
                }
                Console.ResetColor();
            }

            Console.WriteLine();

            // Generate C# code
            var generator = new CSharpCodeGenerator();
            var options = new CodeGenerationOptions
            {
                Namespace = namespaceName,
                OutputDirectory = outputDir.FullName,
                IncludeDocumentation = true,
                UseNullableReferenceTypes = true,
                UseRecords = true,
                UseReadonlyStructs = true
            };

            Console.WriteLine("⚙️  Generating C# code...");

            var input = GenerationInput.From(metadata.DataTypes, metadata.Models);
            var artifacts = generator.GenerateAll(input, options);

            // Create output directory
            if (!outputDir.Exists)
            {
                outputDir.Create();
                Console.WriteLine($"📁 Created directory: {outputDir.FullName}");
            }

            // Write generated files
            var artifactsList = artifacts.ToList();
            Console.WriteLine($"📝 Writing {artifactsList.Count} files...");
            Console.WriteLine();

            foreach (var artifact in artifactsList)
            {
                await artifact.WriteToDiskAsync(outputDir.FullName);
                var relativePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.Combine(outputDir.FullName, artifact.RelativePath));
                Console.WriteLine($"   ✓ {relativePath}");
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"✨ Successfully generated {artifactsList.Count} files in {outputDir.FullName}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Generated types:");
            Console.WriteLine("  - Validated primitives (readonly record structs)");
            Console.WriteLine("  - Validated models (sealed records)");
            Console.WriteLine("  - DTOs (unvalidated boundary types)");
        }
        catch (SpecificationParseException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();

            if (ex.Errors != null && ex.Errors.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Parsing errors:");
                foreach (var error in ex.Errors)
                {
                    Console.WriteLine($"  - {error.Message}");
                    if (!string.IsNullOrWhiteSpace(error.Path))
                    {
                        Console.WriteLine($"    Path: {error.Path}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Error: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
        }
    }
}
