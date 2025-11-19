# Layered Incremental Generation with Metadata Pipeline - Architecture Guide

## Overview

This architecture provides a clear separation of concerns across multiple layers, making the codebase maintainable, testable, and extensible for relationship-aware repository generation.

## Architecture Layers

```
┌─────────────────────────────────────────────────────────────────┐
│                    Layer 1: Source Provider                      │
│  Responsibility: Fast syntax-based filtering and selection       │
│  Technologies: Roslyn SyntaxProvider, Incremental Values        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                  Layer 2: Metadata Extraction                    │
│  Responsibility: Extract structured metadata from symbols        │
│  Components: MetadataExtractor, EntityAnalyzer, RelationshipAnalyzer │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                Layer 3: Metadata Enrichment                      │
│  Responsibility: Analyze relationships, dependencies, cascades   │
│  Components: RelationshipGraphAnalyzer, DependencyResolver      │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                  Layer 4: Code Generation                        │
│  Responsibility: Generate C# code from enriched metadata         │
│  Components: CodeBuilders, Templates, Generators                │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│                    Layer 5: Source Output                        │
│  Responsibility: Emit generated code to compilation             │
│  Technologies: SourceProductionContext.AddSource()              │
└─────────────────────────────────────────────────────────────────┘
```

## Layer Details

### Layer 1: Source Provider (Incremental)

**Purpose**: Efficiently filter syntax nodes and create incremental value providers.

**Key Classes**:
```csharp
// Location: src/NPA.Generators/RepositoryGenerator.cs
public class RepositoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Fast syntax-based predicate
        var repositoryInterfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: IsRepositoryInterfaceCandidate,
                transform: TransformToMetadata)
            .Where(m => m != null);
    }
    
    private static bool IsRepositoryInterfaceCandidate(SyntaxNode node, CancellationToken ct)
    {
        // Quick checks: interface with attributes, name contains "Repository"
        return node is InterfaceDeclarationSyntax { AttributeLists.Count: > 0 } iface
            && iface.Identifier.Text.Contains("Repository");
    }
}
```

**Benefits**:
- ⚡ Minimal allocations
- ⚡ Fast filtering before semantic analysis
- ⚡ Incremental caching

### Layer 2: Metadata Extraction

**Purpose**: Transform Roslyn symbols into structured metadata models.

**Key Classes**:

```csharp
// Location: src/NPA.Generators/Metadata/MetadataExtractor.cs
public static class MetadataExtractor
{
    public static EntityMetadata? ExtractEntityMetadata(INamedTypeSymbol symbol);
    public static PropertyMetadata ExtractPropertyMetadata(IPropertySymbol symbol);
    public static RelationshipMetadata? ExtractRelationshipMetadata(IPropertySymbol symbol);
}

// Location: src/NPA.Generators/Metadata/EntityAnalyzer.cs
public static class EntityAnalyzer
{
    public static EntityInfo AnalyzeEntity(INamedTypeSymbol entitySymbol, Compilation compilation);
    public static List<PropertyInfo> AnalyzeProperties(INamedTypeSymbol entitySymbol);
    public static List<RelationshipInfo> AnalyzeRelationships(INamedTypeSymbol entitySymbol);
}

// Location: src/NPA.Generators/Metadata/RelationshipAnalyzer.cs
public static class RelationshipAnalyzer
{
    public static RelationshipType DetermineType(IPropertySymbol property);
    public static CascadeConfiguration ExtractCascadeConfig(AttributeData[] attributes);
    public static FetchStrategy ExtractFetchStrategy(AttributeData[] attributes);
    public static bool IsOrphanRemovalEnabled(AttributeData[] attributes);
}
```

**Models**:

```csharp
// Location: src/NPA.Generators/Models/EntityMetadata.cs
public class EntityMetadata
{
    public string Name { get; set; }
    public string Namespace { get; set; }
    public string FullName { get; set; }
    public TableInfo Table { get; set; }
    public List<PropertyMetadata> Properties { get; set; }
    public List<RelationshipMetadata> Relationships { get; set; }
    public PrimaryKeyInfo PrimaryKey { get; set; }
}

// Location: src/NPA.Generators/Models/RelationshipMetadata.cs
public class RelationshipMetadata
{
    public string PropertyName { get; set; }
    public string PropertyType { get; set; }
    public RelationshipType Type { get; set; }
    public string TargetEntityType { get; set; }
    public string? MappedBy { get; set; }
    public bool IsOwner => string.IsNullOrEmpty(MappedBy);
    public bool IsBidirectional => !string.IsNullOrEmpty(MappedBy);
    public CascadeConfiguration Cascade { get; set; }
    public FetchStrategy Fetch { get; set; }
    public bool OrphanRemoval { get; set; }
    public JoinColumnInfo? JoinColumn { get; set; }
    public JoinTableInfo? JoinTable { get; set; }
}

// Location: src/NPA.Generators/Models/RepositoryMetadata.cs
public class RepositoryMetadata
{
    public string InterfaceName { get; set; }
    public string ImplementationName { get; set; }
    public string Namespace { get; set; }
    public EntityMetadata Entity { get; set; }
    public string KeyType { get; set; }
    public List<CustomMethodInfo> CustomMethods { get; set; }
    public bool HasRelationships => Entity.Relationships.Any();
}
```

### Layer 3: Metadata Enrichment

**Purpose**: Analyze metadata for dependencies, validate configurations, detect patterns.

**Key Classes**:

```csharp
// Location: src/NPA.Generators/Analysis/RelationshipGraphAnalyzer.cs
public class RelationshipGraphAnalyzer
{
    public RelationshipGraph AnalyzeGraph(List<EntityMetadata> entities);
    public List<EntityMetadata> GetDependencyOrder(EntityMetadata entity);
    public bool HasCircularDependencies(EntityMetadata entity);
    public List<RelationshipMetadata> FindBidirectionalPairs(EntityMetadata entity);
}

// Location: src/NPA.Generators/Analysis/DependencyResolver.cs
public class DependencyResolver
{
    public List<EntityMetadata> ResolveDependencies(EntityMetadata entity, Compilation compilation);
    public OperationOrder DetermineOperationOrder(EntityMetadata entity, OperationType operation);
}

// Location: src/NPA.Generators/Analysis/CascadeAnalyzer.cs
public class CascadeAnalyzer
{
    public CascadePlan AnalyzeCascadeOperations(EntityMetadata entity, CascadeType operation);
    public List<EntityMetadata> GetCascadeTargets(RelationshipMetadata relationship);
    public bool RequiresTransaction(CascadePlan plan);
}

// Location: src/NPA.Generators/Validation/RelationshipValidator.cs
public class RelationshipValidator
{
    public ValidationResult ValidateRelationships(EntityMetadata entity);
    public ValidationResult ValidateBidirectionalConsistency(RelationshipMetadata relationship);
    public ValidationResult ValidateCascadeConfiguration(RelationshipMetadata relationship);
}
```

**Enriched Models**:

```csharp
// Location: src/NPA.Generators/Models/EnrichedRepositoryMetadata.cs
public class EnrichedRepositoryMetadata : RepositoryMetadata
{
    public RelationshipGraph RelationshipGraph { get; set; }
    public List<EntityMetadata> Dependencies { get; set; }
    public Dictionary<string, CascadePlan> CascadePlans { get; set; }
    public List<RelationshipPair> BidirectionalPairs { get; set; }
    public OperationOrderInfo OperationOrder { get; set; }
}
```

### Layer 4: Code Generation

**Purpose**: Generate C# source code from enriched metadata.

**Architecture**:

```csharp
// Location: src/NPA.Generators/CodeGen/RepositoryCodeBuilder.cs
public class RepositoryCodeBuilder
{
    private readonly EnrichedRepositoryMetadata _metadata;
    private readonly StringBuilder _code = new();
    
    public RepositoryCodeBuilder(EnrichedRepositoryMetadata metadata)
    {
        _metadata = metadata;
    }
    
    public RepositoryCodeBuilder AddUsings() { ... return this; }
    public RepositoryCodeBuilder AddNamespace() { ... return this; }
    public RepositoryCodeBuilder AddClassDeclaration() { ... return this; }
    public RepositoryCodeBuilder AddFields() { ... return this; }
    public RepositoryCodeBuilder AddConstructor() { ... return this; }
    public RepositoryCodeBuilder AddBasicCrudMethods() { ... return this; }
    public RepositoryCodeBuilder AddRelationshipLoadMethods() { ... return this; }
    public RepositoryCodeBuilder AddCascadeMethods() { ... return this; }
    public RepositoryCodeBuilder AddQueryMethods() { ... return this; }
    public RepositoryCodeBuilder AddSynchronizationHelpers() { ... return this; }
    
    public string Build() => _code.ToString();
}

// Location: src/NPA.Generators/CodeGen/RelationshipMethodGenerator.cs
public static class RelationshipMethodGenerator
{
    public static string GenerateLoadMethod(RelationshipMetadata relationship, EntityMetadata entity);
    public static string GenerateIncludeMethod(RelationshipMetadata relationship);
    public static string GenerateExistsMethod(RelationshipMetadata relationship);
    public static string GenerateCountMethod(RelationshipMetadata relationship);
}

// Location: src/NPA.Generators/CodeGen/CascadeMethodGenerator.cs
public static class CascadeMethodGenerator
{
    public static string GenerateCascadeInsert(CascadePlan plan, EntityMetadata entity);
    public static string GenerateCascadeUpdate(CascadePlan plan, EntityMetadata entity);
    public static string GenerateCascadeDelete(CascadePlan plan, EntityMetadata entity);
}

// Location: src/NPA.Generators/CodeGen/SynchronizationGenerator.cs
public static class SynchronizationGenerator
{
    public static string GenerateSyncHelper(RelationshipPair bidirectionalPair);
    public static string GenerateAddToCollectionMethod(RelationshipMetadata relationship);
    public static string GenerateRemoveFromCollectionMethod(RelationshipMetadata relationship);
}

// Location: src/NPA.Generators/CodeGen/QueryMethodGenerator.cs
public static class QueryMethodGenerator
{
    public static string GenerateFindByRelationshipMethod(RelationshipMetadata relationship);
    public static string GenerateCountByRelationshipMethod(RelationshipMetadata relationship);
    public static string GenerateAggregateMethod(RelationshipMetadata relationship, AggregateType type);
}
```

**Templates**:

```csharp
// Location: src/NPA.Generators/Templates/RelationshipLoadTemplate.cs
public static class RelationshipLoadTemplate
{
    public static string OneToManyLoad(string methodName, string entityType, ...) => $@"
    public async Task<{entityType}?> {methodName}({keyType} id)
    {{
        const string sql = @""
            SELECT e.*, r.*
            FROM {tableName} e
            LEFT JOIN {relatedTable} r ON r.{foreignKey} = e.{primaryKey}
            WHERE e.{primaryKey} = @id"";
        
        // Dapper mapping logic...
    }}";
    
    public static string ManyToOneLoad(string methodName, ...) => $@"...";
    public static string OneToOneLoad(string methodName, ...) => $@"...";
    public static string ManyToManyLoad(string methodName, ...) => $@"...";
}
```

### Layer 5: Source Output

**Purpose**: Emit generated source to compilation.

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // ... previous layers ...
    
    context.RegisterSourceOutput(enrichedMetadata, (spc, metadata) =>
    {
        try
        {
            var code = new RepositoryCodeBuilder(metadata)
                .AddUsings()
                .AddNamespace()
                .AddClassDeclaration()
                .AddConstructor()
                .AddBasicCrudMethods()
                .AddRelationshipLoadMethods()
                .AddCascadeMethods()
                .AddQueryMethods()
                .Build();
            
            var fileName = $"{metadata.ImplementationName}.g.cs";
            spc.AddSource(fileName, SourceText.From(code, Encoding.UTF8));
        }
        catch (Exception ex)
        {
            spc.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "NPA001",
                    "Repository Generation Error",
                    $"Failed to generate repository: {ex.Message}",
                    "Generation",
                    DiagnosticSeverity.Error,
                    true),
                Location.None));
        }
    });
}
```

## Pipeline Flow Example

```csharp
public void Initialize(IncrementalGeneratorInitializationContext context)
{
    // LAYER 1: Source Provider - Fast filtering
    var repositoryCandidates = context.SyntaxProvider
        .CreateSyntaxProvider(
            predicate: IsRepositoryInterfaceCandidate,
            transform: ExtractRepositorySymbol)
        .Where(s => s != null);
    
    // LAYER 2: Metadata Extraction - Convert symbols to metadata
    var basicMetadata = repositoryCandidates
        .Select((symbol, ct) => MetadataExtractor.ExtractRepositoryMetadata(symbol, ct));
    
    // LAYER 3: Metadata Enrichment - Analyze relationships
    var enrichedMetadata = basicMetadata
        .Combine(context.CompilationProvider)
        .Select((data, ct) => EnrichMetadata(data.Left, data.Right, ct));
    
    // LAYER 4 & 5: Code Generation & Output
    context.RegisterSourceOutput(enrichedMetadata, GenerateRepositoryCode);
}

private static EnrichedRepositoryMetadata EnrichMetadata(
    RepositoryMetadata basic,
    Compilation compilation,
    CancellationToken ct)
{
    var validator = new RelationshipValidator();
    var graphAnalyzer = new RelationshipGraphAnalyzer();
    var cascadeAnalyzer = new CascadeAnalyzer();
    var dependencyResolver = new DependencyResolver();
    
    // Validate
    var validation = validator.ValidateRelationships(basic.Entity);
    if (!validation.IsValid)
        throw new InvalidOperationException(validation.ErrorMessage);
    
    // Analyze
    var graph = graphAnalyzer.AnalyzeGraph(new[] { basic.Entity }.ToList());
    var dependencies = dependencyResolver.ResolveDependencies(basic.Entity, compilation);
    var cascadePlans = basic.Entity.Relationships
        .ToDictionary(
            r => r.PropertyName,
            r => cascadeAnalyzer.AnalyzeCascadeOperations(basic.Entity, r.Cascade.Types));
    
    return new EnrichedRepositoryMetadata
    {
        // Copy basic metadata
        InterfaceName = basic.InterfaceName,
        ImplementationName = basic.ImplementationName,
        Entity = basic.Entity,
        // Add enrichments
        RelationshipGraph = graph,
        Dependencies = dependencies,
        CascadePlans = cascadePlans,
        BidirectionalPairs = graphAnalyzer.FindBidirectionalPairs(basic.Entity)
    };
}
```

## Project Structure

```
src/NPA.Generators/
├── RepositoryGenerator.cs           # Entry point, IIncrementalGenerator
├── EntityMetadataGenerator.cs       # Entry point for entity metadata
│
├── Metadata/                        # Layer 2: Extraction
│   ├── MetadataExtractor.cs        # Core extraction logic
│   ├── EntityAnalyzer.cs           # Entity-specific analysis
│   ├── RelationshipAnalyzer.cs     # Relationship detection
│   └── AttributeReader.cs          # Attribute parsing utilities
│
├── Models/                          # Data models
│   ├── EntityMetadata.cs
│   ├── RelationshipMetadata.cs
│   ├── RepositoryMetadata.cs
│   ├── EnrichedRepositoryMetadata.cs
│   ├── CascadeConfiguration.cs
│   └── ...
│
├── Analysis/                        # Layer 3: Enrichment
│   ├── RelationshipGraphAnalyzer.cs
│   ├── DependencyResolver.cs
│   ├── CascadeAnalyzer.cs
│   └── OperationOrderAnalyzer.cs
│
├── Validation/                      # Layer 3: Validation
│   ├── RelationshipValidator.cs
│   ├── CascadeValidator.cs
│   └── ConfigurationValidator.cs
│
├── CodeGen/                         # Layer 4: Generation
│   ├── RepositoryCodeBuilder.cs    # Main builder
│   ├── RelationshipMethodGenerator.cs
│   ├── CascadeMethodGenerator.cs
│   ├── SynchronizationGenerator.cs
│   ├── QueryMethodGenerator.cs
│   └── SqlGenerator.cs             # SQL query generation
│
├── Templates/                       # Layer 4: Code templates
│   ├── RelationshipLoadTemplate.cs
│   ├── CascadeTemplate.cs
│   ├── QueryTemplate.cs
│   └── HelperTemplate.cs
│
└── Shared/                          # Utilities
    ├── CodeGenHelpers.cs
    ├── NamingConventions.cs
    └── DiagnosticDescriptors.cs
```

## Testing Strategy

```csharp
// Test each layer independently

// Layer 2 tests
public class MetadataExtractorTests : GeneratorTestBase
{
    [Fact]
    public void ExtractRelationshipMetadata_OneToMany_ReturnsCorrectMetadata()
    {
        var source = "...";
        var compilation = CreateCompilation(source);
        var symbol = GetPropertySymbol(compilation, "Orders");
        
        var metadata = MetadataExtractor.ExtractRelationshipMetadata(symbol);
        
        metadata.Type.Should().Be(RelationshipType.OneToMany);
        metadata.TargetEntityType.Should().Be("Order");
    }
}

// Layer 3 tests
public class RelationshipGraphAnalyzerTests
{
    [Fact]
    public void AnalyzeGraph_DetectsCircularDependency()
    {
        var entities = CreateTestEntities();
        var analyzer = new RelationshipGraphAnalyzer();
        
        var graph = analyzer.AnalyzeGraph(entities);
        
        graph.HasCircularDependencies.Should().BeTrue();
    }
}

// Layer 4 tests
public class RepositoryCodeBuilderTests
{
    [Fact]
    public void Build_WithRelationships_GeneratesLoadMethods()
    {
        var metadata = CreateTestMetadata();
        var builder = new RepositoryCodeBuilder(metadata);
        
        var code = builder
            .AddRelationshipLoadMethods()
            .Build();
        
        code.Should().Contain("GetByIdWithOrdersAsync");
    }
}

// Integration tests
public class EndToEndRelationshipTests : GeneratorTestBase
{
    [Fact]
    public void Generate_CompleteRepository_WithAllRelationshipFeatures()
    {
        var source = "... complete entity definitions ...";
        
        var result = RunGenerator<RepositoryGenerator>(source);
        
        result.Diagnostics.Should().BeEmpty();
        var code = GetGeneratedCode(result);
        
        code.Should().Contain("GetByIdWithOrdersAsync");
        code.Should().Contain("CascadeInsert");
        code.Should().Contain("SynchronizeRelationships");
    }
}
```

## Benefits of This Architecture

### 1. Separation of Concerns
- Each layer has a single responsibility
- Easy to understand and modify
- Clear boundaries between layers

### 2. Testability
- Each layer can be tested independently
- Mock dependencies easily
- Fast unit tests

### 3. Maintainability
- Changes isolated to specific layers
- Easy to add new features
- Clear code organization

### 4. Performance
- Incremental regeneration
- Efficient caching
- Minimal allocations

### 5. Extensibility
- Easy to add new relationship types
- Pluggable analyzers
- Template-based generation

## Next Steps

1. **Phase 7.1**: Implement layers 1-4 for basic relationship loading
2. **Phase 7.2**: Extend layer 4 for eager loading and Include methods
3. **Phase 7.3**: Add cascade operation generation in layer 4
4. **Phase 7.4**: Add synchronization generators in layer 4
5. **Phase 7.5**: Add orphan removal logic in layer 3 & 4
6. **Phase 7.6**: Add query method generators in layer 4

Each phase builds on the same architecture, adding new analyzers, validators, and generators without changing the core pipeline.
