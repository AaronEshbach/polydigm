using Polydigm.Metadata;
using System.Globalization;

namespace Polydigm.CodeGeneration.CSharp
{
    /// <summary>
    /// Refines metadata for C# code generation to follow idiomatic naming conventions.
    /// Converts names to PascalCase for types and properties in domain models.
    /// </summary>
    public sealed class CSharpRefiner : CodeRefinerBase
    {
        public CSharpRefiner() : base(CSharpCodeGenerationTarget.Default)
        {
        }

        public CSharpRefiner(CSharpCodeGenerationTarget target) : base(target)
        {
        }

        public override IDataType RefineDataType(IDataType dataType, RefinementContext? context = null)
        {
            // Type names should be PascalCase
            var refinedName = ToPascalCase(dataType.Name);

            if (refinedName == dataType.Name)
            {
                // No changes needed
                return dataType;
            }

            // Create refined version with PascalCase name
            if (dataType is DataTypeMetadata metadata)
            {
                return new DataTypeMetadata
                {
                    Name = refinedName,
                    TypeCode = metadata.TypeCode,
                    Constraints = metadata.Constraints,
                    DefaultValue = metadata.DefaultValue,
                    Description = metadata.Description,
                    Format = metadata.Format,
                    RuntimeType = metadata.RuntimeType,
                    UnderlyingType = metadata.UnderlyingType,
                    IsValidated = metadata.IsValidated,
                    ValidationMethodName = metadata.ValidationMethodName
                };
            }

            // For non-DataTypeMetadata implementations, we can't refine (shouldn't happen in practice)
            return dataType;
        }

        public override IModelMetadata RefineModel(IModelMetadata model, RefinementContext? context = null)
        {
            // Type names should be PascalCase
            var refinedName = ToPascalCase(model.Name);

            // Field names should be PascalCase for domain models
            var refinedFields = model.Fields
                .Select(field => RefineField(field, context))
                .ToList();

            // Check if any changes were made
            var nameChanged = refinedName != model.Name;
            var fieldsChanged = refinedFields.Zip(model.Fields, (refined, original) =>
                refined.Name != original.Name || !ReferenceEquals(refined.DataType, original.DataType))
                .Any(changed => changed);

            if (!nameChanged && !fieldsChanged)
            {
                // No changes needed
                return model;
            }

            // Create refined version
            if (model is ModelMetadata metadata)
            {
                return new ModelMetadata
                {
                    Name = refinedName,
                    Namespace = metadata.Namespace,
                    RuntimeType = metadata.RuntimeType,
                    Description = metadata.Description,
                    Fields = refinedFields,
                    Kind = metadata.Kind,
                    BaseType = metadata.BaseType,
                    Extensions = metadata.Extensions
                };
            }

            // For non-ModelMetadata implementations, we can't refine (shouldn't happen in practice)
            return model;
        }

        private IFieldMetadata RefineField(IFieldMetadata field, RefinementContext? context)
        {
            // Property names should be PascalCase
            var refinedName = ToPascalCase(field.Name);

            // Also refine the field's data type if it's a reference to another validated type
            var refinedDataType = RefineDataType(field.DataType, context);

            // Check if any changes were made
            if (refinedName == field.Name && ReferenceEquals(refinedDataType, field.DataType))
            {
                // No changes needed
                return field;
            }

            // Create refined version
            if (field is FieldMetadata metadata)
            {
                return new FieldMetadata
                {
                    Name = refinedName,
                    DataType = refinedDataType,
                    IsRequired = metadata.IsRequired,
                    IsNullable = metadata.IsNullable,
                    IsReadOnly = metadata.IsReadOnly,
                    Description = metadata.Description,
                    DefaultValue = metadata.DefaultValue,
                    Examples = metadata.Examples,
                    IsCollection = metadata.IsCollection,
                    CollectionElementType = metadata.CollectionElementType != null
                        ? RefineDataType(metadata.CollectionElementType, context)
                        : null
                };
            }

            // For non-FieldMetadata implementations, we can't refine (shouldn't happen in practice)
            return field;
        }

        /// <summary>
        /// Converts a name to PascalCase following C# conventions.
        /// Examples: "userId" -> "UserId", "user_id" -> "UserId", "USER_ID" -> "UserId"
        /// </summary>
        private static string ToPascalCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            // Split on non-alphanumeric characters and underscores
            var parts = name.Split(new[] { '_', '-', ' ', '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                return name;
            }

            // If there's only one part and no separators, check if it's already PascalCase or camelCase
            if (parts.Length == 1 && !name.Contains('_') && !name.Contains('-'))
            {
                // Simple case: just capitalize the first letter
                return CapitalizeFirstLetter(name);
            }

            // Join parts with each part capitalized
            return string.Concat(parts.Select(CapitalizeFirstLetter));
        }

        private static string CapitalizeFirstLetter(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return word;
            }

            // Handle single character
            if (word.Length == 1)
            {
                return word.ToUpper(CultureInfo.InvariantCulture);
            }

            // If the word is all uppercase (like "ID"), keep it as-is (common acronyms)
            if (word.All(char.IsUpper))
            {
                return word;
            }

            // If the word starts with multiple uppercase letters (like "XMLParser"),
            // keep them uppercase (common acronym prefix)
            if (word.Length > 1 && char.IsUpper(word[0]) && char.IsUpper(word[1]))
            {
                return word;
            }

            // Normal case: capitalize first letter, keep rest as-is
            return char.ToUpper(word[0], CultureInfo.InvariantCulture) + word.Substring(1);
        }
    }
}
