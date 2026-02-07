using Microsoft.OpenApi.Models;
using Polydigm.Metadata;
using Polydigm.Specifications;
using System.Text.RegularExpressions;

namespace Polydigm.Specifications.OpenApi
{
    /// <summary>
    /// Extracts Polydigm metadata from OpenAPI documents.
    /// </summary>
    public sealed class OpenApiMetadataExtractor : IMetadataExtractor<OpenApiDocument>
    {
        public IEnumerable<IDataType> ExtractDataTypes(OpenApiDocument specification)
        {
            var dataTypes = new List<IDataType>();

            // Extract from components/schemas
            if (specification.Components?.Schemas != null)
            {
                foreach (var schema in specification.Components.Schemas)
                {
                    var dataType = TryExtractDataType(schema.Key, schema.Value);
                    if (dataType != null && IsSimpleValidatedType(schema.Value))
                    {
                        dataTypes.Add(dataType);
                    }
                }
            }

            return dataTypes;
        }

        public IEnumerable<IModelMetadata> ExtractModels(OpenApiDocument specification)
        {
            var models = new List<IModelMetadata>();

            // Extract from components/schemas
            if (specification.Components?.Schemas != null)
            {
                foreach (var schema in specification.Components.Schemas)
                {
                    var model = TryExtractModel(schema.Key, schema.Value, specification);
                    if (model != null)
                    {
                        models.Add(model);
                    }
                }
            }

            return models;
        }

        public MetadataExtractionResult ExtractAll(OpenApiDocument specification)
        {
            var warnings = new List<string>();
            var dataTypes = new List<IDataType>();
            var models = new List<IModelMetadata>();

            if (specification.Components?.Schemas != null)
            {
                foreach (var schema in specification.Components.Schemas)
                {
                    try
                    {
                        if (IsSimpleValidatedType(schema.Value))
                        {
                            var dataType = TryExtractDataType(schema.Key, schema.Value);
                            if (dataType != null)
                            {
                                dataTypes.Add(dataType);
                            }
                        }
                        else if (IsComplexModel(schema.Value))
                        {
                            var model = TryExtractModel(schema.Key, schema.Value, specification);
                            if (model != null)
                            {
                                models.Add(model);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        warnings.Add($"Failed to extract schema '{schema.Key}': {ex.Message}");
                    }
                }
            }

            return new MetadataExtractionResult
            {
                DataTypes = dataTypes,
                Models = models,
                Warnings = warnings
            };
        }

        // ============ Helper Methods ============

        private bool IsSimpleValidatedType(OpenApiSchema schema)
        {
            // Simple types are primitives with validation constraints
            // (pattern, min/max length, min/max value, etc.)
            if (schema.Type == "object" && schema.Properties?.Count > 0)
                return false;

            return schema.Type is "string" or "integer" or "number"
                && (schema.Pattern != null
                    || schema.MinLength != null
                    || schema.MaxLength != null
                    || schema.Minimum != null
                    || schema.Maximum != null
                    || schema.Format != null);
        }

        private bool IsComplexModel(OpenApiSchema schema)
        {
            return schema.Type == "object" && schema.Properties?.Count > 0;
        }

        private IDataType? TryExtractDataType(string name, OpenApiSchema schema)
        {
            var constraints = new List<IConstraint>();

            // Extract pattern constraint
            if (!string.IsNullOrEmpty(schema.Pattern))
            {
                try
                {
                    constraints.Add(new PatternConstraint(schema.Pattern));
                }
                catch (ArgumentException)
                {
                    // Invalid regex pattern, skip
                }
            }

            // Extract length constraints
            if (schema.MinLength.HasValue)
            {
                constraints.Add(new MinimumLengthConstraint(schema.MinLength.Value));
            }

            if (schema.MaxLength.HasValue)
            {
                constraints.Add(new MaximumLengthConstraint(schema.MaxLength.Value));
            }

            // Extract value constraints
            if (schema.Minimum.HasValue)
            {
                var mode = schema.ExclusiveMinimum == true ? BoundaryMode.Exclusive : BoundaryMode.Inclusive;
                constraints.Add(new MinimumConstraint(schema.Minimum.Value, mode));
            }

            if (schema.Maximum.HasValue)
            {
                var mode = schema.ExclusiveMaximum == true ? BoundaryMode.Exclusive : BoundaryMode.Inclusive;
                constraints.Add(new MaximumConstraint(schema.Maximum.Value, mode));
            }

            // Map OpenAPI type to TypeCode
            var typeCode = MapOpenApiTypeToTypeCode(schema.Type, schema.Format);

            return new DataTypeMetadata
            {
                Name = name,
                TypeCode = typeCode,
                Constraints = constraints,
                Description = schema.Description,
                Format = schema.Format,
                DefaultValue = schema.Default?.ToString(),
                IsValidated = constraints.Count > 0
            };
        }

        private IModelMetadata? TryExtractModel(string name, OpenApiSchema schema, OpenApiDocument document)
        {
            if (!IsComplexModel(schema))
                return null;

            var fields = new List<IFieldMetadata>();

            foreach (var property in schema.Properties)
            {
                var fieldDataType = ResolvePropertyType(property.Value, document);
                if (fieldDataType == null)
                    continue;

                var isRequired = schema.Required?.Contains(property.Key) == true;

                fields.Add(new FieldMetadata
                {
                    Name = property.Key,
                    DataType = fieldDataType,
                    IsRequired = isRequired,
                    IsNullable = property.Value.Nullable || !isRequired,
                    IsReadOnly = property.Value.ReadOnly,
                    Description = property.Value.Description,
                    Examples = property.Value.Example != null ? new[] { property.Value.Example } : null,
                    IsCollection = property.Value.Type == "array",
                    CollectionElementType = property.Value.Type == "array" && property.Value.Items != null
                        ? ResolvePropertyType(property.Value.Items, document)
                        : null
                });
            }

            var kind = DetermineModelKind(name);

            return new ModelMetadata
            {
                Name = name,
                Description = schema.Description,
                Fields = fields,
                Kind = kind
            };
        }

        private IDataType? ResolvePropertyType(OpenApiSchema schema, OpenApiDocument document)
        {
            // Handle references
            if (schema.Reference != null)
            {
                var refName = schema.Reference.Id;
                if (document.Components?.Schemas?.TryGetValue(refName, out var referencedSchema) == true)
                {
                    if (IsSimpleValidatedType(referencedSchema))
                    {
                        return TryExtractDataType(refName, referencedSchema);
                    }
                    else
                    {
                        // For complex types referenced as fields, create a simple reference
                        return new DataTypeMetadata
                        {
                            Name = refName,
                            TypeCode = TypeCode.Object,
                            Description = referencedSchema.Description,
                            IsValidated = false
                        };
                    }
                }
            }

            // Handle inline schemas
            if (IsSimpleValidatedType(schema))
            {
                // Generate a name for inline type
                var inlineName = schema.Title ?? $"Inline{Guid.NewGuid():N}";
                return TryExtractDataType(inlineName, schema);
            }

            // Handle primitive types
            var typeCode = MapOpenApiTypeToTypeCode(schema.Type, schema.Format);
            return new DataTypeMetadata
            {
                Name = schema.Title ?? schema.Type ?? "unknown",
                TypeCode = typeCode,
                Format = schema.Format,
                Description = schema.Description,
                IsValidated = false
            };
        }

        private TypeCode MapOpenApiTypeToTypeCode(string? type, string? format)
        {
            return (type?.ToLowerInvariant(), format?.ToLowerInvariant()) switch
            {
                ("string", "date-time") => TypeCode.DateTime,
                ("string", "date") => TypeCode.DateTime,
                ("string", "byte") => TypeCode.Byte,
                ("string", _) => TypeCode.String,
                ("integer", "int32") => TypeCode.Int32,
                ("integer", "int64") => TypeCode.Int64,
                ("integer", _) => TypeCode.Int32,
                ("number", "float") => TypeCode.Single,
                ("number", "double") => TypeCode.Double,
                ("number", _) => TypeCode.Decimal,
                ("boolean", _) => TypeCode.Boolean,
                ("array", _) => TypeCode.Object,
                ("object", _) => TypeCode.Object,
                _ => TypeCode.String
            };
        }

        private ModelKind DetermineModelKind(string name)
        {
            var lowerName = name.ToLowerInvariant();

            if (lowerName.EndsWith("request") || lowerName.EndsWith("command"))
                return ModelKind.Request;

            if (lowerName.EndsWith("response") || lowerName.EndsWith("result"))
                return ModelKind.Response;

            if (lowerName.EndsWith("dto"))
                return ModelKind.Dto;

            if (lowerName.EndsWith("event"))
                return ModelKind.Event;

            return ModelKind.Entity;
        }
    }
}
