using Polydigm.Metadata;

namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Generates validated model types (sealed records that compose validated primitives).
    /// Example: TestModel with TestId, TestType, TestName fields.
    /// </summary>
    public sealed class ValidatedModelGenerator
    {
        private readonly CodeGenerationOptions options;

        public ValidatedModelGenerator(CodeGenerationOptions? options = null)
        {
            this.options = options ?? CodeGenerationOptions.Default;
        }

        public string Generate(IModelMetadata model)
        {
            var builder = new CSharpCodeBuilder();

            // File header
            if (!string.IsNullOrWhiteSpace(options.FileHeaderTemplate))
            {
                builder.AppendLine(options.FileHeaderTemplate);
                builder.AppendLine();
            }

            // Using statements
            builder.AppendLine("using Polydigm.Metadata;");
            builder.AppendLine();

            // Namespace
            if (!string.IsNullOrWhiteSpace(options.Namespace))
            {
                builder.AppendLine($"namespace {options.Namespace}");
                builder.AppendLine("{");
            }

            // XML documentation
            builder.AppendXmlDoc(model.Description ?? $"Validated {model.Name} model.");

            // Attribute - indicate this is validated with DTO as underlying type
            var dtoNamespace = string.IsNullOrWhiteSpace(options.Namespace)
                ? "DTO"
                : $"{options.Namespace}.DTO";
            builder.AppendLine($"[Validated(typeof({dtoNamespace}.{model.Name}))]");

            // Type declaration
            builder.AppendLine($"public sealed record {model.Name}");
            builder.AppendLine("{");

            // Properties (using validated types, not primitives!)
            foreach (var field in model.Fields)
            {
                if (!string.IsNullOrWhiteSpace(field.Description))
                {
                    builder.AppendXmlDoc(field.Description);
                }

                var propertyType = field.DataType.Name; // Use the validated type name
                var requiredModifier = field.IsRequired ? "required " : "";
                builder.AppendLine($"public {requiredModifier}{propertyType} {field.Name} {{ get; init; }}");
                builder.AppendLine();
            }

            // TryCreate method (from DTO)
            GenerateTryCreateMethod(builder, model, dtoNamespace);
            builder.AppendLine();

            // Create method (from DTO)
            GenerateCreateMethod(builder, model, dtoNamespace);
            builder.AppendLine();

            // ToDTO method (convert back to DTO)
            GenerateToDtoMethod(builder, model, dtoNamespace);

            // Close type
            builder.AppendLine("}");

            // Close namespace
            if (!string.IsNullOrWhiteSpace(options.Namespace))
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private void GenerateTryCreateMethod(CSharpCodeBuilder builder, IModelMetadata model, string dtoNamespace)
        {
            builder.AppendLine($"public static bool TryCreate({dtoNamespace}.{model.Name} dto, out {model.Name}? validated)");
            builder.AppendLine("{");

            // Try to create each field
            var fieldValidations = new List<string>();
            foreach (var field in model.Fields)
            {
                var varName = ToCamelCase(field.Name);
                fieldValidations.Add($"{field.DataType.Name}.TryCreate(dto.{field.Name}, out var {varName})");
            }

            // Build validation condition
            builder.AppendLine($"if ({string.Join(" &&", fieldValidations.Select(v => $"\n        {v}"))})");
            builder.AppendLine("{");

            // Create validated model
            builder.AppendLine($"validated = new {model.Name}");
            builder.AppendLine("{");
            foreach (var field in model.Fields)
            {
                var varName = ToCamelCase(field.Name);
                builder.AppendLine($"{field.Name} = {varName},");
            }
            builder.AppendLine("};");
            builder.AppendLine();
            builder.AppendLine("return true;");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("validated = default;");
            builder.AppendLine("return false;");
            builder.AppendLine("}");
        }

        private void GenerateCreateMethod(CSharpCodeBuilder builder, IModelMetadata model, string dtoNamespace)
        {
            builder.AppendLine("[Validation]");
            builder.AppendLine($"public static {model.Name} Create({dtoNamespace}.{model.Name} dto)");
            builder.AppendLine("{");
            builder.AppendLine($"return new {model.Name}");
            builder.AppendLine("{");

            foreach (var field in model.Fields)
            {
                builder.AppendLine($"{field.Name} = {field.DataType.Name}.Create(dto.{field.Name}),");
            }

            builder.AppendLine("};");
            builder.AppendLine("}");
        }

        private void GenerateToDtoMethod(CSharpCodeBuilder builder, IModelMetadata model, string dtoNamespace)
        {
            builder.AppendLine($"public static {dtoNamespace}.{model.Name} ToDTO({model.Name} model)");
            builder.AppendLine("{");
            builder.AppendLine($"return new {dtoNamespace}.{model.Name}");
            builder.AppendLine("{");

            foreach (var field in model.Fields)
            {
                // Access the .Value property of validated wrappers
                builder.AppendLine($"{field.Name} = model.{field.Name}.Value,");
            }

            builder.AppendLine("};");
            builder.AppendLine("}");
        }

        private string ToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
                return name;

            return char.ToLowerInvariant(name[0]) + name.Substring(1);
        }
    }
}
