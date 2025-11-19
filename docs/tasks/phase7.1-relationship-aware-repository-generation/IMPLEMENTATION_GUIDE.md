# Phase 7.1 Implementation Guide: Relationship-Aware Repository Generation

## Architecture: Layered Incremental Generation with Metadata Pipeline

This guide implements the **5-layer architecture** for relationship-aware repository generation. See [ARCHITECTURE.md](./ARCHITECTURE.md) for detailed architecture overview.

## Layer Implementation Overview

```
Layer 1: Source Provider      → Fast syntax filtering
Layer 2: Metadata Extraction  → Convert symbols to models
Layer 3: Metadata Enrichment  → Analyze & validate
Layer 4: Code Generation      → Generate C# source
Layer 5: Source Output        → Emit to compilation
```

## Step-by-Step Implementation

## Layer 1: Source Provider (Already Implemented ✅)

Your existing `RepositoryGenerator.Initialize()` already implements this layer effectively.

**Current Implementation**:
```csharp
// src/NPA.Generators/RepositoryGenerator.cs (lines 20-40)
var repositoryInterfaces = context.SyntaxProvider
    .CreateSyntaxProvider(
        predicate: static (node, _) => IsRepositoryInterface(node),
        transform: static (ctx, _) => GetRepositoryInfo(ctx))
    .Where(static info => info is not null);
```

**No changes needed** - This already provides fast incremental filtering.

## Layer 2: Metadata Extraction

### 2.1 Create Relationship Metadata Models

```csharp
public static class MetadataExtractor
{
    // Add new method
    public static RelationshipMetadata ExtractRelationshipMetadata(IPropertySymbol property)
    {
        var attributes = property.GetAttributes();
        
        var relationshipType = DetermineRelationshipType(attributes);
        if (relationshipType == RelationshipType.None)
            return null;
            
        return new RelationshipMetadata
        {
            PropertyName = property.Name,
            PropertyType = property.Type.ToDisplayString(),
            RelationshipType = relationshipType,
            MappedBy = GetMappedBy(attributes),
            CascadeTypes = ExtractCascadeTypes(attributes),
            FetchType = ExtractFetchType(attributes),
            OrphanRemoval = HasOrphanRemoval(attributes),
            JoinColumn = ExtractJoinColumn(attributes),
            JoinTable = ExtractJoinTable(attributes),
            IsCollection = IsCollectionType(property.Type),
            TargetEntityType = GetTargetEntityType(property.Type)
        };
    }
    
    private static RelationshipType DetermineRelationshipType(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            var name = attr.AttributeClass?.Name;
            if (name == "OneToManyAttribute") return RelationshipType.OneToMany;
            if (name == "ManyToOneAttribute") return RelationshipType.ManyToOne;
            if (name == "OneToOneAttribute") return RelationshipType.OneToOne;
            if (name == "ManyToManyAttribute") return RelationshipType.ManyToMany;
        }
        return RelationshipType.None;
    }
    
    private static CascadeType ExtractCascadeTypes(ImmutableArray<AttributeData> attributes)
    {
        var cascadeAttr = attributes.FirstOrDefault(a => 
            a.AttributeClass?.Name == "CascadeAttribute");
            
        if (cascadeAttr == null)
            return CascadeType.None;
            
        // Extract from constructor argument or named argument
        if (cascadeAttr.ConstructorArguments.Length > 0)
        {
            var value = cascadeAttr.ConstructorArguments[0].Value;
            if (value is int intValue)
                return (CascadeType)intValue;
        }
        
        return CascadeType.None;
    }
}
```

### 2.1 Create Relationship Metadata Models

**Location**: `src/NPA.Generators/Models/RelationshipMetadata.cs`

```csharp
namespace NPA.Generators.Models;

public enum RelationshipType
{
    None,
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany
}

[Flags]
public enum CascadeType
{
    None = 0,
    Persist = 1 << 0,
    Update = 1 << 1,
    Remove = 1 << 2,
    Merge = 1 << 3,
    Refresh = 1 << 4,
    All = Persist | Update | Remove | Merge | Refresh
}

public enum FetchType
{
    Lazy,
    Eager
}

public class RelationshipMetadata
{
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public RelationshipType Type { get; set; }
    public string TargetEntityType { get; set; } = string.Empty;
    public string? MappedBy { get; set; }
    public CascadeType CascadeTypes { get; set; }
    public FetchType FetchType { get; set; }
    public bool OrphanRemoval { get; set; }
    public JoinColumnInfo? JoinColumn { get; set; }
    public JoinTableInfo? JoinTable { get; set; }
    
    // Computed properties
    public bool IsCollection { get; set; }
    public bool IsOwner => string.IsNullOrEmpty(MappedBy);
    public bool IsBidirectional => !string.IsNullOrEmpty(MappedBy);
    public bool RequiresCascade => CascadeTypes != CascadeType.None;
}

public class JoinColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string? ReferencedColumnName { get; set; }
    public bool Nullable { get; set; } = true;
}

public class JoinTableInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public string[] JoinColumns { get; set; } = Array.Empty<string>();
    public string[] InverseJoinColumns { get; set; } = Array.Empty<string>();
}
```

### 2.2 Enhance MetadataExtractor

**Location**: `src/NPA.Generators/Shared/MetadataExtractor.cs`

Add new method to extract relationship metadata:

```csharp
namespace NPA.Generators.Models;

public class RelationshipMetadata
{
    public string PropertyName { get; set; }
    public string PropertyType { get; set; }
    public RelationshipType RelationshipType { get; set; }
    public string? MappedBy { get; set; }
    public CascadeType CascadeTypes { get; set; }
    public FetchType FetchType { get; set; }
    public bool OrphanRemoval { get; set; }
    public JoinColumnInfo? JoinColumn { get; set; }
    public JoinTableInfo? JoinTable { get; set; }
    public bool IsCollection { get; set; }
    public string TargetEntityType { get; set; }
    public bool IsBidirectional => !string.IsNullOrEmpty(MappedBy);
    public bool IsOwner => string.IsNullOrEmpty(MappedBy);
}

public enum RelationshipType
{
    None,
    OneToOne,
    OneToMany,
    ManyToOne,
    ManyToMany
}

[Flags]
public enum CascadeType
{
    None = 0,
    Persist = 1 << 0,
    Update = 1 << 1,
    Remove = 1 << 2,
    Merge = 1 << 3,
    Refresh = 1 << 4,
    All = Persist | Update | Remove | Merge | Refresh
}

public enum FetchType
{
    Lazy,
    Eager
}
```

## 3. Enhance RepositoryGenerator

### Location: `src/NPA.Generators/RepositoryGenerator.cs`

```csharp
public class RepositoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Existing code...
        
        var repositoryInterfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsRepositoryInterface(node),
                transform: static (ctx, _) => GetRepositoryInfoWithRelationships(ctx))
            .Where(static info => info is not null);
            
        // Rest of initialization...
    }
    
    private static RepositoryInfo? GetRepositoryInfoWithRelationships(GeneratorSyntaxContext context)
    {
        // Existing extraction logic...
        
        // NEW: Extract entity relationships
        var entitySymbol = GetEntitySymbol(entityType, context.SemanticModel.Compilation);
        var relationships = entitySymbol?.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(p => MetadataExtractor.ExtractRelationshipMetadata(p))
            .Where(r => r != null)
            .ToList() ?? new List<RelationshipMetadata>();
        
        return new RepositoryInfo
        {
            // Existing fields...
            Relationships = relationships, // NEW
            HasRelationships = relationships.Any(),
            EagerRelationships = relationships.Where(r => r.FetchType == FetchType.Eager).ToList()
        };
    }
}
```

## 4. Create Relationship Code Generators

### Location: `src/NPA.Generators/CodeGen/RelationshipCodeGenerator.cs`

```csharp
namespace NPA.Generators.CodeGen;

public static class RelationshipCodeGenerator
{
    public static string GenerateGetWithRelationshipsMethod(
        RepositoryInfo repository,
        RelationshipMetadata relationship)
    {
        return relationship.RelationshipType switch
        {
            RelationshipType.OneToMany => GenerateOneToManyLoad(repository, relationship),
            RelationshipType.ManyToOne => GenerateManyToOneLoad(repository, relationship),
            RelationshipType.OneToOne => GenerateOneToOneLoad(repository, relationship),
            RelationshipType.ManyToMany => GenerateManyToManyLoad(repository, relationship),
            _ => string.Empty
        };
    }
    
    private static string GenerateOneToManyLoad(
        RepositoryInfo repository, 
        RelationshipMetadata relationship)
    {
        var methodName = $"GetByIdWith{relationship.PropertyName}Async";
        var entityType = repository.EntityType;
        var keyType = repository.KeyType;
        
        return $@"
    public async Task<{entityType}?> {methodName}({keyType} id)
    {{
        const string sql = @""
            SELECT 
                e.*,
                r.*
            FROM {repository.TableName} e
            LEFT JOIN {GetRelatedTableName(relationship)} r 
                ON r.{GetForeignKeyColumn(relationship)} = e.{repository.PrimaryKeyColumn}
            WHERE e.{repository.PrimaryKeyColumn} = @id"";
        
        var entityDict = new Dictionary<{keyType}, {entityType}>();
        
        await _connection.QueryAsync<{entityType}, {relationship.TargetEntityType}, {entityType}>(
            sql,
            (entity, related) =>
            {{
                if (!entityDict.TryGetValue(entity.{repository.PrimaryKeyProperty}, out var existingEntity))
                {{
                    existingEntity = entity;
                    existingEntity.{relationship.PropertyName} = new List<{relationship.TargetEntityType}>();
                    entityDict.Add(entity.{repository.PrimaryKeyProperty}, existingEntity);
                }}
                
                if (related != null)
                    existingEntity.{relationship.PropertyName}.Add(related);
                
                return existingEntity;
            }},
            new {{ id }},
            splitOn: ""id"");
        
        return entityDict.Values.FirstOrDefault();
    }}";
    }
}
```

## 5. Modify Code Generation in GenerateRepository

### Location: `src/NPA.Generators/RepositoryGenerator.cs`

```csharp
private static void GenerateRepository(SourceProductionContext context, RepositoryInfo info)
{
    var builder = new StringBuilder();
    
    // Existing: namespace, using, class declaration
    GenerateNamespaceAndUsings(builder, info);
    GenerateClassDeclaration(builder, info);
    
    // Existing: constructor, basic CRUD
    GenerateConstructor(builder, info);
    GenerateBasicCrudMethods(builder, info);
    
    // NEW: Relationship methods
    if (info.HasRelationships)
    {
        GenerateRelationshipLoadMethods(builder, info);
        GenerateCascadeOperations(builder, info);
        GenerateQueryByRelationshipMethods(builder, info);
    }
    
    // Existing: custom methods, close class
    GenerateCustomMethods(builder, info);
    builder.AppendLine("}");
    
    context.AddSource($"{info.ImplementationName}.g.cs", builder.ToString());
}

private static void GenerateRelationshipLoadMethods(StringBuilder builder, RepositoryInfo info)
{
    foreach (var relationship in info.Relationships)
    {
        var code = RelationshipCodeGenerator.GenerateGetWithRelationshipsMethod(info, relationship);
        builder.AppendLine(code);
    }
    
    // Generate method to load all eager relationships
    if (info.EagerRelationships.Any())
    {
        var code = RelationshipCodeGenerator.GenerateGetWithAllEagerRelationships(info);
        builder.AppendLine(code);
    }
}
```

## 6. Testing Approach

### Location: `tests/NPA.Generators.Tests/RelationshipAwareRepositoryTests.cs`

```csharp
public class RelationshipAwareRepositoryTests : GeneratorTestBase
{
    [Fact]
    public void Generate_WithOneToManyRelationship_GeneratesLoadMethod()
    {
        var source = @"
using NPA.Core.Annotations;
using NPA.Core.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

[Entity, Table(""customers"")]
public class Customer
{
    [Id]
    public int Id { get; set; }
    public string Name { get; set; }
    
    [OneToMany(MappedBy = ""Customer"")]
    [Fetch(FetchType.Eager)]
    public ICollection<Order> Orders { get; set; }
}

[Entity, Table(""orders"")]
public class Order
{
    [Id]
    public int Id { get; set; }
    
    [ManyToOne]
    [JoinColumn(""customer_id"")]
    public Customer Customer { get; set; }
}

[Repository]
public interface ICustomerRepository : IRepository<Customer, int> { }";

        var result = RunGenerator<RepositoryGenerator>(source);
        
        result.Diagnostics.Should().BeEmpty();
        var code = GetGeneratedCode(result);
        
        code.Should().Contain(""GetByIdWithOrdersAsync"");
        code.Should().Contain(""LEFT JOIN orders"");
        code.Should().Contain(""customer_id"");
    }
}
```

## Implementation Order

1. ✅ **Week 1**: Metadata models and extraction
   - Create RelationshipMetadata model
   - Enhance MetadataExtractor
   - Add relationship detection to RepositoryGenerator
   
2. ✅ **Week 2**: Basic relationship loading
   - Implement RelationshipCodeGenerator
   - Generate GetByIdWith[Relationship]Async methods
   - Handle eager fetch configuration
   
3. ✅ **Week 3**: Testing and refinement
   - Comprehensive test coverage
   - Performance optimization
   - Documentation

## Key Design Decisions

### Why Incremental Generator?
- ✅ Already in use and working well
- ✅ Excellent performance for large solutions
- ✅ Proper caching and change detection
- ✅ Integrates with existing infrastructure

### Why Metadata Pipeline?
- ✅ Separation of concerns
- ✅ Testable components
- ✅ Reusable across generators
- ✅ Easy to extend for new features

### Why Code Builder Pattern?
- ✅ Readable code generation
- ✅ Composable generation logic
- ✅ Easy to test individual parts
- ✅ Maintainable over time

## Next Steps

After Phase 7.1, this pattern continues for:
- **Phase 7.2**: Add Include() method generation using same pattern
- **Phase 7.3**: Add cascade operation generation in GenerateRepository
- **Phase 7.4**: Add synchronization helper classes
- **Phase 7.5**: Add orphan removal logic to Update methods
- **Phase 7.6**: Add query method generation for relationships
