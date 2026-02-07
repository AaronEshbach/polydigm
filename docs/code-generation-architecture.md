# Polydigm Code Generation Architecture

## Overview

The Polydigm code generation system is designed with **complete abstraction from any specific specification format or target language**. The metadata model serves as a universal bridge between any specification format (OpenAPI, JSON Schema, AsyncAPI, etc.) and any target language (C#, TypeScript, Python, etc.).

## Architecture Principles

1. **Format Agnostic**: Core framework has zero dependencies on specific spec formats
2. **Language Agnostic**: Code generation abstractions work for any target language
3. **Universal Metadata Model**: All specs convert to the same metadata representation
4. **Plugin Architecture**: New spec formats and languages can be added without modifying core
5. **Composability**: Mix and match parsers and generators freely

## Data Flow

```
┌──────────────────────┐
│  Specification       │
│  (OpenAPI, JSON      │
│   Schema, AsyncAPI,  │
│   WSDL, etc.)        │
└──────┬───────────────┘
       │
       │ Parse
       ▼
┌──────────────────────┐
│  ISpecificationParser│
│  <TSpec>             │
└──────┬───────────────┘
       │
       │ Extract
       ▼
┌──────────────────────┐
│  Metadata Model      │
│  ┌────────────────┐  │
│  │  IDataType     │  │
│  │  IModelMetadata│  │
│  │  IConstraint   │  │
│  └────────────────┘  │
└──────┬───────────────┘
       │
       │ Generate
       ▼
┌──────────────────────┐
│  ICodeGenerator      │
└──────┬───────────────┘
       │
       │ Output
       ▼
┌──────────────────────┐
│  Generated Code      │
│  (C#, TypeScript,    │
│   Python, etc.)      │
└──────────────────────┘
```

## Package Structure

### Core Packages (No Dependencies on Specific Formats)

#### 1. Polydigm.Metadata.Abstractions
**Purpose**: Universal metadata model that all specs convert to

**Key Types**:
- `IMetadataService` - Runtime metadata registry
- `IDataType` - Represents validated types and primitives
- `IModelMetadata` - Represents complex types
- `IFieldMetadata` - Represents fields within models
- `IConstraint` - Validation constraints (Pattern, Range, Length, etc.)

**Dependencies**: None (pure abstractions)

#### 2. Polydigm.Metadata
**Purpose**: Implementation of metadata service with reflection

**Key Types**:
- `MetadataService` - Concrete implementation with caching
- Concrete implementations of all metadata interfaces

**Dependencies**: Polydigm.Metadata.Abstractions

#### 3. Polydigm.Specifications.Abstractions
**Purpose**: Abstraction layer for specification parsing

**Key Interfaces**:
- `ISpecificationParser<TSpec>` - Parse any spec format
- `ISpecificationFormat` - Describes a spec format
- `ISpecificationSource` - Where specs come from (file, URL, string)
- `IMetadataExtractor<TSpec>` - Extract metadata from parsed specs
- `ISpecificationProcessor<TSpec>` - Combined parser + extractor

**Concrete Utilities**:
- `FileSpecificationSource`, `UrlSpecificationSource`, `StringSpecificationSource`
- `SpecificationValidationResult`, `SpecificationParseException`

**Dependencies**: Polydigm.Metadata.Abstractions

#### 4. Polydigm.CodeGeneration.Abstractions
**Purpose**: Abstraction layer for code generation

**Key Interfaces**:
- `ICodeGenerator` - Generate code in any language
- `ICodeGenerationTarget` - Describes target language/framework
- `IGeneratedArtifact` - Represents generated code

**Configuration**:
- `CodeGenerationOptions` - Language-agnostic options with extension points

**Dependencies**: Polydigm.Metadata.Abstractions

### Implementation Packages (Format/Language Specific)

These packages implement the abstractions for specific formats and languages:

#### Polydigm.Specifications.OpenApi (Future)
- `OpenApiParser` : `ISpecificationParser<OpenApiDocument>`
- `OpenApiMetadataExtractor` : `IMetadataExtractor<OpenApiDocument>`
- Converts OpenAPI 3.x specs to metadata model

#### Polydigm.Specifications.JsonSchema (Future)
- `JsonSchemaParser` : `ISpecificationParser<JsonSchema>`
- `JsonSchemaMetadataExtractor` : `IMetadataExtractor<JsonSchema>`
- Converts JSON Schema to metadata model

#### Polydigm.Specifications.AsyncApi (Future)
- `AsyncApiParser` : `ISpecificationParser<AsyncApiDocument>`
- `AsyncApiMetadataExtractor` : `IMetadataExtractor<AsyncApiDocument>`
- Converts AsyncAPI specs to metadata model

#### Polydigm.CodeGeneration.CSharp (Future)
- `CSharpCodeGenerator` : `ICodeGenerator`
- Generates C# code from metadata model
- Supports validated types, models, and enums

#### Polydigm.CodeGeneration.TypeScript (Future)
- `TypeScriptCodeGenerator` : `ICodeGenerator`
- Generates TypeScript code from metadata model

## Usage Examples

### Example 1: OpenAPI → C# Code Generation

```csharp
// Parse OpenAPI spec
var openApiParser = new OpenApiParser();
var spec = await openApiParser.ParseAsync(
    SpecificationSource.FromFile("api.yaml")
);

// Extract metadata
var extractor = new OpenApiMetadataExtractor();
var metadata = extractor.ExtractAll(spec);

// Register with metadata service
var metadataService = new MetadataService();
metadata.RegisterWith(metadataService);

// Generate C# code
var csharpGenerator = new CSharpCodeGenerator();
var options = new CodeGenerationOptions
{
    Namespace = "MyApp.Models",
    OutputDirectory = "Models",
    UseRecords = true,
    UseReadonlyStructs = true
};

var input = GenerationInput.From(metadata.DataTypes, metadata.Models);
var artifacts = csharpGenerator.GenerateAll(input, options);

// Write to disk
foreach (var artifact in artifacts)
{
    await artifact.WriteToDiskAsync("src/");
}
```

### Example 2: JSON Schema → TypeScript Generation

```csharp
// Parse JSON Schema
var jsonSchemaParser = new JsonSchemaParser();
var schema = await jsonSchemaParser.ParseAsync(
    SpecificationSource.FromUrl("https://example.com/schema.json")
);

// Extract metadata
var extractor = new JsonSchemaMetadataExtractor();
var metadata = extractor.ExtractAll(schema);

// Generate TypeScript code
var tsGenerator = new TypeScriptCodeGenerator();
var options = new CodeGenerationOptions
{
    Namespace = "models",  // module name in TS
    OutputDirectory = "src/models"
};

var input = GenerationInput.From(metadata.DataTypes, metadata.Models);
var artifacts = tsGenerator.GenerateAll(input, options);

// Write to disk
foreach (var artifact in artifacts)
{
    await artifact.WriteToDiskAsync("./");
}
```

### Example 3: Using ISpecificationProcessor (Simplified API)

```csharp
// Create processor for OpenAPI
var processor = new OpenApiSpecificationProcessor();

// Parse and extract in one step
var metadata = await processor.ProcessAsync(
    SpecificationSource.FromFile("api.yaml")
);

// Generate code
var generator = new CSharpCodeGenerator();
var input = GenerationInput.From(metadata.DataTypes, metadata.Models);
var artifacts = generator.GenerateAll(input);

foreach (var artifact in artifacts)
{
    await artifact.WriteToDiskAsync("Models/");
}
```

### Example 4: Multiple Formats → Single Codebase

```csharp
var metadataService = new MetadataService();

// Import from OpenAPI
var openApiProcessor = new OpenApiSpecificationProcessor();
var apiMetadata = await openApiProcessor.ProcessAsync(
    SpecificationSource.FromFile("api.yaml")
);
apiMetadata.RegisterWith(metadataService);

// Import from JSON Schema
var jsonSchemaProcessor = new JsonSchemaProcessor();
var schemaMetadata = await jsonSchemaProcessor.ProcessAsync(
    SpecificationSource.FromFile("domain-types.json")
);
schemaMetadata.RegisterWith(metadataService);

// Generate unified C# codebase
var generator = new CSharpCodeGenerator();
var allDataTypes = metadataService.GetAllDataTypes();
var allModels = metadataService.GetAllModels();

var input = GenerationInput.From(allDataTypes, allModels);
var artifacts = generator.GenerateAll(input);

foreach (var artifact in artifacts)
{
    await artifact.WriteToDiskAsync("src/Models/");
}
```

## Extension Points

### Adding a New Specification Format

1. Create a package: `Polydigm.Specifications.{FormatName}`
2. Define your spec type: `class MySpec { ... }`
3. Implement `ISpecificationParser<MySpec>`
4. Implement `IMetadataExtractor<MySpec>`
5. Optionally implement `ISpecificationProcessor<MySpec>`

No changes to core needed!

### Adding a New Target Language

1. Create a package: `Polydigm.CodeGeneration.{Language}`
2. Define your target: `class MyLanguageTarget : ICodeGenerationTarget`
3. Implement `ICodeGenerator`
4. Implement artifact generation for:
   - `GenerateDataType(IDataType)`
   - `GenerateModel(IModelMetadata)`

No changes to core needed!

## Specification Format Implementations

### Priority Order

1. **OpenAPI 3.0/3.1** (Highest Priority)
   - Most common REST API specification format
   - Excellent tooling ecosystem
   - Good mapping to Polydigm models

2. **JSON Schema** (High Priority)
   - Foundation for many other formats
   - Direct mapping to data types
   - Referenced by OpenAPI

3. **AsyncAPI** (Medium Priority)
   - Event-driven/messaging APIs
   - Similar structure to OpenAPI
   - Growing adoption

4. **Protobuf/gRPC** (Future)
   - High-performance RPC
   - Strong typing
   - Popular in microservices

5. **GraphQL Schema** (Future)
   - API query language
   - Schema definition language
   - Growing adoption

## Code Generation Target Implementations

### Priority Order

1. **C#** (Highest Priority)
   - Primary framework language
   - Generate validated types following Polydigm patterns
   - Support for records, readonly structs, and modern C# features

2. **TypeScript** (High Priority)
   - Common frontend language
   - Enables full-stack type safety
   - Good tooling support

3. **Python** (Medium Priority)
   - Popular for data science and backend
   - Type hints support
   - dataclasses for models

4. **Go** (Future)
   - Microservices
   - Strong typing
   - Growing adoption

## Benefits of This Architecture

### 1. Zero Coupling
- Core framework never imports OpenAPI, JSON Schema, or any specific format
- Each format is a self-contained plugin
- Formats can be added/removed without affecting core

### 2. Mix and Match
- Use OpenAPI parser with TypeScript generator
- Use JSON Schema parser with C# generator
- Use any parser with any generator

### 3. Consistency
- All formats produce the same metadata model
- Generated code is consistent regardless of source format
- Validation rules preserved across formats

### 4. Future-Proof
- New spec formats can be added without breaking changes
- New languages can be added without modifying parsers
- Community can contribute format support

### 5. Testability
- Each component can be tested in isolation
- Mock parsers for testing generators
- Mock generators for testing parsers

## Implementation Status

✅ **Completed**:
- Core metadata model (IDataType, IModelMetadata, IConstraint)
- Metadata service with reflection
- Specification parser abstractions
- Code generator abstractions
- Source utilities (File, URL, String, Stream)

🔜 **Next Steps**:
1. Implement OpenAPI parser (`Polydigm.Specifications.OpenApi`)
2. Implement C# code generator (`Polydigm.CodeGeneration.CSharp`)
3. Create dotnet tool that uses both
4. Add more formats (JSON Schema, AsyncAPI)
5. Add more languages (TypeScript, Python)

## Design Decisions

### Why Generic TSpec?
- Allows each parser to work with native spec types
- OpenAPI parser works with OpenApiDocument
- JSON Schema parser works with JsonSchema
- No forced common representation at parse time

### Why Separate Parser and Extractor?
- Parser handles format-specific deserialization
- Extractor handles metadata extraction logic
- Can replace parser without changing extraction
- Can share extractors across similar formats

### Why ISpecificationProcessor?
- Convenience API for common case
- Hides parser/extractor split when not needed
- Single call for parse + extract
- Optional - advanced users can use parser/extractor directly

### Why Not Use AutoMapper or Similar?
- Metadata extraction is not simple property mapping
- Requires validation logic, constraint detection
- Needs spec-specific knowledge (e.g., OpenAPI $ref resolution)
- Custom logic is more maintainable

## File Organization

```
Polydigm/
├── Polydigm.Metadata.Abstractions/    # Core metadata model
├── Polydigm.Metadata/                 # Metadata service implementation
├── Polydigm.Specifications.Abstractions/  # Spec parsing abstractions
├── Polydigm.CodeGeneration.Abstractions/  # Code gen abstractions
├── Polydigm.Specifications.OpenApi/   # OpenAPI parser (future)
├── Polydigm.Specifications.JsonSchema/  # JSON Schema parser (future)
├── Polydigm.Specifications.AsyncApi/    # AsyncAPI parser (future)
├── Polydigm.CodeGeneration.CSharp/      # C# generator (future)
├── Polydigm.CodeGeneration.TypeScript/  # TypeScript generator (future)
└── Polydigm.CodeGen.Tool/               # dotnet tool (future)
```

Each package is:
- Self-contained
- Independently versioned
- Testable in isolation
- Optional (except core abstractions)
