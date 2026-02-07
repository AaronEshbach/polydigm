# Polydigm Metadata Service

The Metadata Service is the foundation for bidirectional code generation and runtime introspection in the Polydigm framework.

## Overview

The metadata service provides:
- **Runtime introspection** of validated types and complex models
- **Reflection-based extraction** of type metadata from existing code
- **Registration system** for manually-defined or generated type metadata
- **Foundation for spec generation** (OpenAPI, JSON Schema, etc.)

## Architecture

### Packages

#### Polydigm.Metadata.Abstractions
Contains interfaces and abstractions for the metadata system:
- `IMetadataService` - Core service interface
- `IDataType` - Represents validated types and primitives
- `IModelMetadata` - Represents complex types (classes/records with fields)
- `IFieldMetadata` - Represents fields/properties within models
- `IConstraint` - Base for validation constraints
  - `IPatternConstraint` - Regex pattern validation
  - `IMinimumConstraint` / `IMaximumConstraint` - Range validation
  - `IMinimumLengthConstraint` / `IMaximumLengthConstraint` - Length validation
  - `IRequiredConstraint` - Required field validation

#### Polydigm.Metadata
Contains the default implementation:
- `MetadataService` - Thread-safe implementation with caching
- `DataTypeMetadata` - Concrete implementation of `IDataType`
- `ModelMetadata` - Concrete implementation of `IModelMetadata`
- `FieldMetadata` - Concrete implementation of `IFieldMetadata`
- Concrete constraint implementations (`PatternConstraint`, `MinimumConstraint`, etc.)

## Usage

### Basic Usage

```csharp
var metadataService = new MetadataService();

// Scan an assembly for types marked with [Validated]
metadataService.ScanAssembly(typeof(MyApp.Models.TestId).Assembly);

// Get metadata for a validated type
var testIdMetadata = metadataService.GetDataType<TestId>();
Console.WriteLine($"Type: {testIdMetadata.Name}");
Console.WriteLine($"Underlying Type: {testIdMetadata.TypeCode}");
foreach (var constraint in testIdMetadata.Constraints)
{
    if (constraint is IPatternConstraint pattern)
    {
        Console.WriteLine($"Pattern: {pattern.Pattern}");
    }
}

// Get metadata for a complex model
var userMetadata = metadataService.GetModelMetadata<User>();
Console.WriteLine($"Model: {userMetadata.Name}");
foreach (var field in userMetadata.Fields)
{
    Console.WriteLine($"  {field.Name}: {field.DataType.Name} (Required: {field.IsRequired})");
}
```

### Extracting Metadata from Existing Types

```csharp
var metadataService = new MetadataService();

// Extract metadata via reflection
var dataType = metadataService.TryExtractDataType(typeof(TestId));
if (dataType != null)
{
    // Successfully extracted metadata
    Console.WriteLine($"Extracted: {dataType.Name}");
}

// Extract model metadata
var model = metadataService.TryExtractModelMetadata(typeof(User));
if (model != null)
{
    Console.WriteLine($"Model has {model.Fields.Count} fields");
}
```

### Manual Registration

```csharp
var metadataService = new MetadataService();

// Register metadata for a type not yet generated
var emailType = new DataTypeMetadata
{
    Name = "Email",
    TypeCode = TypeCode.String,
    Constraints = new[]
    {
        new PatternConstraint(@"^[^@]+@[^@]+\.[^@]+$")
    },
    Description = "Email address",
    Format = "email",
    IsValidated = true
};

metadataService.RegisterDataType(emailType);

// Later, retrieve it
var retrieved = metadataService.GetDataType("Email");
```

## How It Works

### Reflection-Based Extraction

The `MetadataService` can extract metadata from types using reflection:

1. **Validated Types**: Looks for `[Validated]` attribute
   - Extracts underlying type from attribute
   - Finds static fields/properties marked with `[Pattern]` and extracts regex
   - Finds static methods marked with `[Validation]` or named `TryCreate`
   - Builds constraint list

2. **Complex Models**: Analyzes classes/records
   - Extracts all public properties and fields
   - Determines data type for each field (recursively)
   - Detects collections and their element types
   - Infers nullable/required status
   - Categorizes models by naming convention (Request, Response, Dto, etc.)

### Caching

The metadata service maintains internal caches for performance:
- Type-to-metadata mappings
- Name-to-metadata mappings
- Thread-safe concurrent dictionaries

## Next Steps

The metadata service enables the following features:

### 1. OpenAPI Spec Generation
```csharp
var openApiGenerator = new OpenApiGenerator(metadataService);
var spec = openApiGenerator.GenerateSpecification();
// Write spec to openapi.yaml
```

### 2. Code Generation from Specs
```csharp
var openApiParser = new OpenApiParser();
var metadata = openApiParser.Parse("openapi.yaml");

// Register parsed metadata
foreach (var dataType in metadata.DataTypes)
{
    metadataService.RegisterDataType(dataType);
}

// Generate code
var codeGenerator = new CSharpCodeGenerator(metadataService);
codeGenerator.GenerateValidatedTypes("Models/");
```

### 3. JSON Schema Generation
```csharp
var jsonSchemaGenerator = new JsonSchemaGenerator(metadataService);
var schema = jsonSchemaGenerator.Generate<User>();
```

### 4. Runtime Documentation
```csharp
// Provide detailed error messages using metadata
var metadata = metadataService.GetDataType<Email>();
throw new ValidationException(
    $"Value does not match pattern: {((IPatternConstraint)metadata.Constraints[0]).Pattern}"
);
```

## Design Principles

1. **Separation of Concerns**: Abstractions separate from implementation
2. **Bidirectional**: Same metadata model for both code→spec and spec→code
3. **Extensible**: Easy to add new constraint types or metadata properties
4. **Thread-Safe**: Can be used in concurrent scenarios
5. **Lazy Loading**: Metadata extracted on-demand, cached for performance
6. **DI-Friendly**: Injectable service for clean architecture

## Examples

See `Polydigm.Validation.Tests/TestModels.cs` for an example of a validated type that the metadata service can introspect:

```csharp
[Validated]
public readonly record struct TestId
{
    [Pattern]
    private static readonly Regex Pattern = new(@"^[a-zA-Z0-9]{6}-[a-zA-Z0-9]{6}$");

    private readonly string value;
    public string Value => value;

    private TestId(string value) { this.value = value; }

    [Validation]
    public static bool TryCreate(string input, out TestId validated)
    {
        if (Pattern.IsMatch(input))
        {
            validated = new TestId(input);
            return true;
        }
        validated = default;
        return false;
    }
}
```

The metadata service will automatically extract:
- Type name: "TestId"
- Underlying type: String (TypeCode.String)
- Pattern constraint: `^[a-zA-Z0-9]{6}-[a-zA-Z0-9]{6}$`
- Validation method: "TryCreate"
