using Polydigm.Metadata;
using System.Text.RegularExpressions;

namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Generates validated primitive wrapper types (readonly record structs).
    /// Example: TestId, TestName, etc.
    /// </summary>
    public sealed class ValidatedPrimitiveGenerator
    {
        private readonly CodeGenerationOptions options;

        public ValidatedPrimitiveGenerator(CodeGenerationOptions? options = null)
        {
            this.options = options ?? CodeGenerationOptions.Default;
        }

        public string Generate(IDataType dataType)
        {
            var builder = new CSharpCodeBuilder();
            var underlyingTypeName = GetUnderlyingTypeName(dataType.TypeCode);

            // File header
            if (!string.IsNullOrWhiteSpace(options.FileHeaderTemplate))
            {
                builder.AppendLine(options.FileHeaderTemplate);
                builder.AppendLine();
            }

            // Using statements
            builder.AppendLine("using Polydigm.Metadata;");
            if (HasPatternConstraint(dataType))
            {
                builder.AppendLine("using System.Text.RegularExpressions;");
            }
            builder.AppendLine();

            // Namespace
            if (!string.IsNullOrWhiteSpace(options.Namespace))
            {
                builder.AppendLine($"namespace {options.Namespace}");
                builder.AppendLine("{");
            }

            // XML documentation
            builder.AppendXmlDoc(dataType.Description ?? $"Validated {dataType.Name} value.");

            // Attribute
            builder.AppendLine("[Validated]");

            // Type declaration
            builder.AppendLine($"public readonly record struct {dataType.Name}");
            builder.AppendLine("{");

            // Generate constraint fields/properties
            GenerateConstraintMembers(builder, dataType);

            // Private value field
            builder.AppendLine($"private readonly {underlyingTypeName} value;");
            builder.AppendLine();

            // Public Value property
            builder.AppendLine($"public {underlyingTypeName} Value => value;");
            builder.AppendLine();

            // Private constructor
            builder.AppendLine($"private {dataType.Name}({underlyingTypeName} value)");
            builder.AppendLine("{");
            builder.AppendLine("this.value = value;");
            builder.AppendLine("}");
            builder.AppendLine();

            // TryCreate method
            GenerateTryCreateMethod(builder, dataType, underlyingTypeName);
            builder.AppendLine();

            // Create method
            GenerateCreateMethod(builder, dataType, underlyingTypeName);
            builder.AppendLine();

            // ToString override
            builder.AppendLine($"public override string ToString() => value{(dataType.TypeCode == TypeCode.String ? "" : ".ToString()")};");

            // Close type
            builder.AppendLine("}");

            // Close namespace
            if (!string.IsNullOrWhiteSpace(options.Namespace))
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private void GenerateConstraintMembers(CSharpCodeBuilder builder, IDataType dataType)
        {
            foreach (var constraint in dataType.Constraints)
            {
                if (constraint is IPatternConstraint pattern)
                {
                    builder.AppendLine("[Pattern]");
                    var regexPattern = pattern.Pattern.ToString().Replace("\"", "\\\"");
                    builder.AppendLine($"private static readonly Regex Pattern = new(@\"{regexPattern}\", RegexOptions.Compiled);");
                    builder.AppendLine();
                }
                else if (constraint is IMaximumLengthConstraint maxLength)
                {
                    builder.AppendLine("[MaxLength]");
                    builder.AppendLine($"private const int MaxLength = {maxLength.MaximumLength};");
                    builder.AppendLine();
                }
                else if (constraint is IMinimumLengthConstraint minLength)
                {
                    builder.AppendLine("[MinLength]");
                    builder.AppendLine($"private const int MinLength = {minLength.MinimumLength};");
                    builder.AppendLine();
                }
                else if (constraint is IMaximumConstraint max)
                {
                    builder.AppendLine("[Maximum]");
                    builder.AppendLine($"private const {GetUnderlyingTypeName(dataType.TypeCode)} Maximum = {max.Maximum};");
                    builder.AppendLine();
                }
                else if (constraint is IMinimumConstraint min)
                {
                    builder.AppendLine("[Minimum]");
                    builder.AppendLine($"private const {GetUnderlyingTypeName(dataType.TypeCode)} Minimum = {min.Minimum};");
                    builder.AppendLine();
                }
                else if (constraint is IRequiredConstraint)
                {
                    builder.AppendLine("[Required]");
                    builder.AppendLine("private const bool IsRequired = true;");
                    builder.AppendLine();
                }
            }
        }

        private void GenerateTryCreateMethod(CSharpCodeBuilder builder, IDataType dataType, string underlyingTypeName)
        {
            var isNullable = options.UseNullableReferenceTypes && dataType.TypeCode == TypeCode.String;
            var inputType = isNullable ? $"{underlyingTypeName}?" : underlyingTypeName;

            builder.AppendLine($"public static bool TryCreate({inputType} input, out {dataType.Name} validated)");
            builder.AppendLine("{");

            // Build validation condition
            var validationChecks = new List<string>();

            // Null check for strings
            if (dataType.TypeCode == TypeCode.String)
            {
                var hasRequired = dataType.Constraints.Any(c => c is IRequiredConstraint);
                if (hasRequired || !isNullable)
                {
                    validationChecks.Add("!string.IsNullOrWhiteSpace(input)");
                }
                else
                {
                    validationChecks.Add("input is not null");
                }
            }

            // Pattern constraint
            if (HasPatternConstraint(dataType))
            {
                validationChecks.Add("Pattern.IsMatch(input)");
            }

            // Length constraints
            foreach (var constraint in dataType.Constraints)
            {
                if (constraint is IMinimumLengthConstraint)
                {
                    validationChecks.Add("input.Length >= MinLength");
                }
                else if (constraint is IMaximumLengthConstraint)
                {
                    validationChecks.Add("input.Length <= MaxLength");
                }
                else if (constraint is IMinimumConstraint)
                {
                    validationChecks.Add("input >= Minimum");
                }
                else if (constraint is IMaximumConstraint)
                {
                    validationChecks.Add("input <= Maximum");
                }
            }

            // Generate validation logic
            if (validationChecks.Count > 0)
            {
                builder.AppendLine($"if ({string.Join(" && ", validationChecks)})");
            }
            else
            {
                builder.AppendLine("if (input is not null)");
            }

            builder.AppendLine("{");
            builder.AppendLine($"validated = new {dataType.Name}(input);");
            builder.AppendLine("return true;");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine("validated = default;");
            builder.AppendLine("return false;");
            builder.AppendLine("}");
        }

        private void GenerateCreateMethod(CSharpCodeBuilder builder, IDataType dataType, string underlyingTypeName)
        {
            var isNullable = options.UseNullableReferenceTypes && dataType.TypeCode == TypeCode.String;
            var inputType = isNullable ? $"{underlyingTypeName}?" : underlyingTypeName;

            builder.AppendLine("[Validation]");
            builder.AppendLine($"public static {dataType.Name} Create({inputType} input)");
            builder.AppendLine("{");
            builder.AppendLine("if (TryCreate(input, out var validated))");
            builder.AppendLine("{");
            builder.AppendLine("return validated;");
            builder.AppendLine("}");
            builder.AppendLine();
            builder.AppendLine($"throw new ValidationException<{inputType}, {dataType.Name}>(input);");
            builder.AppendLine("}");
        }

        private bool HasPatternConstraint(IDataType dataType)
        {
            return dataType.Constraints.Any(c => c is IPatternConstraint);
        }

        private string GetUnderlyingTypeName(TypeCode typeCode)
        {
            return typeCode switch
            {
                TypeCode.String => "string",
                TypeCode.Int32 => "int",
                TypeCode.Int64 => "long",
                TypeCode.Decimal => "decimal",
                TypeCode.Double => "double",
                TypeCode.Single => "float",
                TypeCode.Boolean => "bool",
                TypeCode.DateTime => "DateTime",
                TypeCode.Byte => "byte",
                _ => "string"
            };
        }
    }
}
