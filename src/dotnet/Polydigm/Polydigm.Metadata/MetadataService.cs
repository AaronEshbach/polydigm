using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;
using Polydigm.Validation;

namespace Polydigm.Metadata
{
    /// <summary>
    /// Default implementation of IMetadataService using reflection and a registry.
    /// Thread-safe for concurrent reads and writes.
    /// </summary>
    public class MetadataService : IMetadataService
    {
        private readonly ConcurrentDictionary<string, IDataType> dataTypes = new();
        private readonly ConcurrentDictionary<string, IModelMetadata> models = new();
        private readonly ConcurrentDictionary<Type, IDataType> typeToDataType = new();
        private readonly ConcurrentDictionary<Type, IModelMetadata> typeToModel = new();

        // ============ Data Type Methods ============

        public IDataType GetDataType(Type type)
        {
            if (typeToDataType.TryGetValue(type, out var cached))
                return cached;

            var extracted = TryExtractDataType(type);
            if (extracted == null)
                throw new InvalidOperationException($"Type {type.FullName} is not a valid Polydigm data type.");

            return extracted;
        }

        public IDataType GetDataType<T>() => GetDataType(typeof(T));

        public IDataType? GetDataType(string typeName)
        {
            return dataTypes.TryGetValue(typeName, out var dataType) ? dataType : null;
        }

        public IEnumerable<IDataType> GetAllDataTypes() => dataTypes.Values;

        public IEnumerable<IDataType> GetValidatedTypes()
        {
            return dataTypes.Values.Where(dt => dt is DataTypeMetadata dtm && dtm.IsValidated);
        }

        public void RegisterDataType(IDataType dataType)
        {
            dataTypes[dataType.Name] = dataType;
            if (dataType is DataTypeMetadata dtm && dtm.RuntimeType != null)
            {
                typeToDataType[dtm.RuntimeType] = dataType;
            }
        }

        // ============ Model Metadata Methods ============

        public IModelMetadata? GetModelMetadata(Type type)
        {
            if (typeToModel.TryGetValue(type, out var cached))
                return cached;

            return TryExtractModelMetadata(type);
        }

        public IModelMetadata? GetModelMetadata<T>() => GetModelMetadata(typeof(T));

        public IModelMetadata? GetModelMetadata(string modelName)
        {
            return models.TryGetValue(modelName, out var model) ? model : null;
        }

        public IEnumerable<IModelMetadata> GetAllModels() => models.Values;

        public void RegisterModel(IModelMetadata model)
        {
            models[model.Name] = model;
            if (model.RuntimeType != null)
            {
                typeToModel[model.RuntimeType] = model;
            }
        }

        // ============ Reflection-Based Discovery ============

        public void ScanAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                // Try to extract as validated type
                var dataType = TryExtractDataType(type);
                if (dataType != null)
                {
                    RegisterDataType(dataType);
                }

                // Try to extract as model
                var model = TryExtractModelMetadata(type);
                if (model != null)
                {
                    RegisterModel(model);
                }
            }
        }

        public IDataType? TryExtractDataType(Type type)
        {
            // Check if already cached
            if (typeToDataType.TryGetValue(type, out var cached))
                return cached;

            // Check for [Validated] attribute
            var validatedAttr = type.GetCustomAttribute<ValidatedAttribute>();
            if (validatedAttr == null)
            {
                // Not a validated type, could be a primitive
                return TryCreatePrimitiveDataType(type);
            }

            // Extract validated type metadata
            var constraints = ExtractConstraints(type);
            var underlyingType = validatedAttr.UnderlyingType ?? typeof(string);
            var validationMethod = FindValidationMethod(type);

            var dataType = new DataTypeMetadata
            {
                Name = type.Name,
                TypeCode = Type.GetTypeCode(underlyingType),
                Constraints = constraints,
                Description = ExtractDescription(type),
                Format = ExtractFormat(type),
                RuntimeType = type,
                UnderlyingType = underlyingType,
                IsValidated = true,
                ValidationMethodName = validationMethod
            };

            // Cache it
            typeToDataType[type] = dataType;
            dataTypes[type.Name] = dataType;

            return dataType;
        }

        public IModelMetadata? TryExtractModelMetadata(Type type)
        {
            // Check if already cached
            if (typeToModel.TryGetValue(type, out var cached))
                return cached;

            // Must be a class or struct with properties/fields
            if (!type.IsClass && !type.IsValueType)
                return null;

            // Skip validated types (they're data types, not models)
            if (type.GetCustomAttribute<ValidatedAttribute>() != null)
                return null;

            // Skip primitive types
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
                return null;

            // Extract fields/properties
            var fields = ExtractFields(type);
            if (fields.Count == 0)
                return null; // Not a model if it has no fields

            var model = new ModelMetadata
            {
                Name = type.Name,
                Namespace = type.Namespace,
                RuntimeType = type,
                Description = ExtractDescription(type),
                Fields = fields,
                Kind = DetermineModelKind(type),
                BaseType = type.BaseType?.Name
            };

            // Cache it
            typeToModel[type] = model;
            models[type.Name] = model;

            return model;
        }

        // ============ Helper Methods ============

        private IReadOnlyList<IConstraint> ExtractConstraints(Type type)
        {
            var constraints = new List<IConstraint>();

            // Look for Pattern attribute on static fields/properties
            var members = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var member in members)
            {
                var patternAttr = member.GetCustomAttribute<PatternAttribute>();
                if (patternAttr != null)
                {
                    Regex? regex = null;
                    if (member is FieldInfo field && field.FieldType == typeof(Regex))
                    {
                        regex = field.GetValue(null) as Regex;
                    }
                    else if (member is PropertyInfo prop && prop.PropertyType == typeof(Regex))
                    {
                        regex = prop.GetValue(null) as Regex;
                    }

                    if (regex != null)
                    {
                        constraints.Add(new PatternConstraint(regex));
                    }
                }
            }

            // Look for other constraint attributes (future expansion)
            // Could add MinimumAttribute, MaximumAttribute, etc.

            return constraints;
        }

        private string? FindValidationMethod(Type type)
        {
            // Look for [Validation] attribute on static methods
            var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (var method in methods)
            {
                if (method.GetCustomAttribute<ValidationAttribute>() != null)
                {
                    return method.Name;
                }
            }

            // Default convention: look for "TryCreate"
            if (type.GetMethod("Create", BindingFlags.Static | BindingFlags.Public) != null)
            {
                return "Create";
            }

            return null;
        }

        private IReadOnlyList<IFieldMetadata> ExtractFields(Type type)
        {
            var fields = new List<IFieldMetadata>();

            // Extract properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                var propDataType = GetOrCreateDataType(prop.PropertyType);
                if (propDataType == null)
                    continue;

                var isNullable = IsNullableType(prop.PropertyType);
                var isCollection = IsCollectionType(prop.PropertyType, out var elementType);

                fields.Add(new FieldMetadata
                {
                    Name = prop.Name,
                    DataType = propDataType,
                    IsRequired = !isNullable,
                    IsNullable = isNullable,
                    IsReadOnly = prop.SetMethod == null,
                    Description = ExtractDescription(prop),
                    IsCollection = isCollection,
                    CollectionElementType = elementType != null ? GetOrCreateDataType(elementType) : null
                });
            }

            // Extract public fields (less common but supported)
            var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fieldInfos)
            {
                var fieldDataType = GetOrCreateDataType(field.FieldType);
                if (fieldDataType == null)
                    continue;

                var isNullable = IsNullableType(field.FieldType);
                var isCollection = IsCollectionType(field.FieldType, out var elementType);

                fields.Add(new FieldMetadata
                {
                    Name = field.Name,
                    DataType = fieldDataType,
                    IsRequired = !isNullable,
                    IsNullable = isNullable,
                    IsReadOnly = field.IsInitOnly,
                    Description = null,
                    IsCollection = isCollection,
                    CollectionElementType = elementType != null ? GetOrCreateDataType(elementType) : null
                });
            }

            return fields;
        }

        private IDataType? GetOrCreateDataType(Type type)
        {
            // Try to get from cache first
            if (typeToDataType.TryGetValue(type, out var cached))
                return cached;

            // Try to extract
            var extracted = TryExtractDataType(type);
            if (extracted != null)
                return extracted;

            // For primitives, create on the fly
            return TryCreatePrimitiveDataType(type);
        }

        private IDataType? TryCreatePrimitiveDataType(Type type)
        {
            if (!IsPrimitiveOrWellKnown(type))
                return null;

            var dataType = new DataTypeMetadata
            {
                Name = type.Name,
                TypeCode = Type.GetTypeCode(type),
                Constraints = Array.Empty<IConstraint>(),
                RuntimeType = type,
                IsValidated = false
            };

            typeToDataType[type] = dataType;
            return dataType;
        }

        private bool IsPrimitiveOrWellKnown(Type type)
        {
            return type.IsPrimitive
                || type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(Guid);
        }

        private bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null
                || (!type.IsValueType);
        }

        private bool IsCollectionType(Type type, out Type? elementType)
        {
            elementType = null;

            if (type.IsArray)
            {
                elementType = type.GetElementType();
                return true;
            }

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                if (genericDef == typeof(List<>) ||
                    genericDef == typeof(IList<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(IReadOnlyList<>) ||
                    genericDef == typeof(IReadOnlyCollection<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            return false;
        }

        private ModelKind DetermineModelKind(Type type)
        {
            var name = type.Name.ToLowerInvariant();

            if (name.EndsWith("request") || name.EndsWith("command"))
                return ModelKind.Request;

            if (name.EndsWith("response") || name.EndsWith("result"))
                return ModelKind.Response;

            if (name.EndsWith("dto"))
                return ModelKind.Dto;

            if (name.EndsWith("event"))
                return ModelKind.Event;

            return ModelKind.Entity;
        }

        private string? ExtractDescription(MemberInfo member)
        {
            // TODO: Extract from XML documentation comments
            // This would require parsing the XML docs file at runtime or using Roslyn
            return null;
        }

        private string? ExtractFormat(Type type)
        {
            // Could infer format from type name or attributes
            // E.g., "Email" -> "email", "Uri" -> "uri"
            var name = type.Name.ToLowerInvariant();

            if (name.Contains("email"))
                return "email";
            if (name.Contains("uri") || name.Contains("url"))
                return "uri";
            if (name.Contains("uuid") || name.Contains("guid"))
                return "uuid";
            if (name.Contains("date"))
                return "date-time";

            return null;
        }
    }
}
