using Polydigm.Metadata;

namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Generates DTO types (unvalidated records with nullable properties).
    /// These are used as the input boundary for validated models.
    /// </summary>
    public sealed class DtoGenerator
    {
        private readonly CodeGenerationOptions options;

        public DtoGenerator(CodeGenerationOptions? options = null)
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

            // DTO namespace (separate from validated types)
            var dtoNamespace = string.IsNullOrWhiteSpace(options.Namespace)
                ? "DTO"
                : $"{options.Namespace}.DTO";

            builder.AppendLine($"namespace {dtoNamespace}");
            builder.AppendLine("{");

            // XML documentation
            builder.AppendXmlDoc(model.Description ?? $"Data transfer object for {model.Name}.");

            // Type declaration
            builder.AppendLine($"public record {model.Name}");
            builder.AppendLine("{");

            // Properties (all nullable strings for simplicity)
            foreach (var field in model.Fields)
            {
                if (!string.IsNullOrWhiteSpace(field.Description))
                {
                    builder.AppendXmlDoc(field.Description);
                }

                // All DTO fields are nullable strings (primitive boundary)
                var propertyType = GetDtoPropertyType(field);
                builder.AppendLine($"public {propertyType} {field.Name} {{ get; init; }}");
                builder.AppendLine();
            }

            // Close type
            builder.AppendLine("}");

            // Close namespace
            builder.AppendLine("}");

            return builder.ToString();
        }

        private string GetDtoPropertyType(IFieldMetadata field)
        {
            // DTOs use primitives, not validated wrappers
            // All are nullable for leniency at the boundary
            return field.DataType.TypeCode switch
            {
                TypeCode.String => "string?",
                TypeCode.Int32 => "int?",
                TypeCode.Int64 => "long?",
                TypeCode.Decimal => "decimal?",
                TypeCode.Double => "double?",
                TypeCode.Single => "float?",
                TypeCode.Boolean => "bool?",
                TypeCode.DateTime => "DateTime?",
                _ => "string?"
            };
        }
    }
}
