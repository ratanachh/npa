# Quick Start: Layered Incremental Generation Implementation

## Phase 7.1 - Week 1 Tasks

This guide provides a concrete implementation plan for the first week of Phase 7.1.

## Day 1-2: Setup Layer 2 (Metadata Models & Extraction)

### Task 1.1: Create Relationship Models (2 hours)

**File**: `src/NPA.Generators/Models/RelationshipMetadata.cs`

```bash
# Create the file
cd src/NPA.Generators/Models
# Add RelationshipMetadata.cs with the content from IMPLEMENTATION_GUIDE.md
```

**Checklist**:
- [ ] Create `RelationshipType` enum
- [ ] Create `CascadeType` enum (with Flags)
- [ ] Create `FetchType` enum
- [ ] Create `RelationshipMetadata` class
- [ ] Create `JoinColumnInfo` class
- [ ] Create `JoinTableInfo` class
- [ ] Build and verify no errors

### Task 1.2: Extend MetadataExtractor (4 hours)

**File**: `src/NPA.Generators/Shared/MetadataExtractor.cs`

Add this new method:

```csharp
public static RelationshipMetadata? ExtractRelationshipMetadata(IPropertySymbol property)
{
    var attributes = property.GetAttributes();
    
    // Determine relationship type
    var relType = DetermineRelationshipType(attributes);
    if (relType == RelationshipType.None)
        return null;
    
    // Extract all metadata
    return new RelationshipMetadata
    {
        PropertyName = property.Name,
        PropertyType = property.Type.ToDisplayString(),
        Type = relType,
        TargetEntityType = GetTargetEntityType(property.Type),
        MappedBy = GetMappedBy(attributes),
        CascadeTypes = ExtractCascadeTypes(attributes),
        FetchType = ExtractFetchType(attributes),
        OrphanRemoval = HasOrphanRemoval(attributes),
        JoinColumn = ExtractJoinColumn(attributes),
        JoinTable = ExtractJoinTable(attributes),
        IsCollection = IsCollectionType(property.Type)
    };
}

// Helper methods (implement each)
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

private static string GetTargetEntityType(ITypeSymbol typeSymbol)
{
    // For collections like ICollection<Order>, extract Order
    if (typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType)
    {
        return namedType.TypeArguments.FirstOrDefault()?.ToDisplayString() ?? typeSymbol.ToDisplayString();
    }
    return typeSymbol.ToDisplayString();
}

private static bool IsCollectionType(ITypeSymbol typeSymbol)
{
    return typeSymbol is INamedTypeSymbol { IsGenericType: true } namedType
        && (namedType.Name == "ICollection" || namedType.Name == "IList" || namedType.Name == "List");
}

// Implement remaining helper methods...
```

**Checklist**:
- [ ] Add `ExtractRelationshipMetadata` method
- [ ] Implement `DetermineRelationshipType`
- [ ] Implement `GetTargetEntityType`
- [ ] Implement `IsCollectionType`
- [ ] Implement `GetMappedBy`
- [ ] Implement `ExtractCascadeTypes`
- [ ] Implement `ExtractFetchType`
- [ ] Implement `HasOrphanRemoval`
- [ ] Implement `ExtractJoinColumn`
- [ ] Implement `ExtractJoinTable`
- [ ] Build and verify

### Task 1.3: Write Unit Tests (2 hours)

**File**: `tests/NPA.Generators.Tests/MetadataExtractorTests.cs`

```csharp
public class MetadataExtractorTests : GeneratorTestBase
{
    [Fact]
    public void ExtractRelationshipMetadata_OneToMany_ReturnsCorrectType()
    {
        var source = @"
using NPA.Core.Annotations;
using System.Collections.Generic;

public class Customer
{
    [OneToMany(MappedBy = ""Customer"")]
    public ICollection<Order> Orders { get; set; }
}

public class Order { }";

        var compilation = CreateCompilation(source);
        var property = GetPropertySymbol(compilation, "Customer", "Orders");
        
        var metadata = MetadataExtractor.ExtractRelationshipMetadata(property);
        
        metadata.Should().NotBeNull();
        metadata.Type.Should().Be(RelationshipType.OneToMany);
        metadata.PropertyName.Should().Be("Orders");
        metadata.MappedBy.Should().Be("Customer");
        metadata.IsCollection.Should().BeTrue();
    }
    
    // Add more tests for each relationship type
}
```

## Day 3-4: Integrate with RepositoryGenerator (Layer 2)

### Task 2.1: Update RepositoryInfo Model (1 hour)

**File**: `src/NPA.Generators/RepositoryGenerator.cs` (in RepositoryInfo class)

```csharp
private class RepositoryInfo
{
    // Existing properties...
    
    // NEW: Add relationship support
    public List<RelationshipMetadata> Relationships { get; set; } = new();
    public bool HasRelationships => Relationships.Any();
    public List<RelationshipMetadata> EagerRelationships => 
        Relationships.Where(r => r.FetchType == FetchType.Eager).ToList();
}
```

### Task 2.2: Extract Relationships in GetRepositoryInfo (2 hours)

**File**: `src/NPA.Generators/RepositoryGenerator.cs`

Update `GetRepositoryInfo` method:

```csharp
private static RepositoryInfo? GetRepositoryInfo(GeneratorSyntaxContext context)
{
    // ... existing code to get interfaceSymbol, entityType, keyType ...
    
    // NEW: Extract entity relationships
    var entitySymbol = GetEntitySymbol(entityType, context.SemanticModel.Compilation);
    var relationships = new List<RelationshipMetadata>();
    
    if (entitySymbol != null)
    {
        foreach (var property in entitySymbol.GetMembers().OfType<IPropertySymbol>())
        {
            var relationship = MetadataExtractor.ExtractRelationshipMetadata(property);
            if (relationship != null)
            {
                relationships.Add(relationship);
            }
        }
    }
    
    return new RepositoryInfo
    {
        // ... existing fields ...
        Relationships = relationships, // NEW
        // ... rest of fields ...
    };
}

// Helper method to get entity symbol
private static INamedTypeSymbol? GetEntitySymbol(string entityTypeName, Compilation compilation)
{
    return compilation.GetTypeByMetadataName(entityTypeName);
}
```

**Checklist**:
- [ ] Update `RepositoryInfo` class
- [ ] Modify `GetRepositoryInfo` to extract relationships
- [ ] Add `GetEntitySymbol` helper
- [ ] Build and verify no errors

## Day 5: Setup Layer 4 (Code Generation Infrastructure)

### Task 3.1: Create Code Builder (3 hours)

**File**: `src/NPA.Generators/CodeGen/RepositoryCodeBuilder.cs`

```csharp
namespace NPA.Generators.CodeGen;

public class RepositoryCodeBuilder
{
    private readonly RepositoryInfo _info;
    private readonly StringBuilder _code = new();
    
    public RepositoryCodeBuilder(RepositoryInfo info)
    {
        _info = info;
    }
    
    public RepositoryCodeBuilder AddUsings()
    {
        _code.AppendLine("using System;");
        _code.AppendLine("using System.Collections.Generic;");
        _code.AppendLine("using System.Data;");
        _code.AppendLine("using System.Linq;");
        _code.AppendLine("using System.Threading.Tasks;");
        _code.AppendLine("using Dapper;");
        _code.AppendLine("using NPA.Core.Repositories;");
        
        if (_info.HasRelationships)
        {
            _code.AppendLine("using NPA.Core.Metadata;");
        }
        
        _code.AppendLine();
        return this;
    }
    
    public RepositoryCodeBuilder AddNamespace()
    {
        _code.AppendLine($"namespace {_info.Namespace}");
        _code.AppendLine("{");
        return this;
    }
    
    public RepositoryCodeBuilder AddClassDeclaration()
    {
        _code.AppendLine($"    public class {_info.ImplementationName} : {_info.InterfaceName}");
        _code.AppendLine("    {");
        return this;
    }
    
    public RepositoryCodeBuilder AddFields()
    {
        _code.AppendLine("        private readonly IDbConnection _connection;");
        _code.AppendLine();
        return this;
    }
    
    public RepositoryCodeBuilder AddConstructor()
    {
        _code.AppendLine($"        public {_info.ImplementationName}(IDbConnection connection)");
        _code.AppendLine("        {");
        _code.AppendLine("            _connection = connection;");
        _code.AppendLine("        }");
        _code.AppendLine();
        return this;
    }
    
    public RepositoryCodeBuilder AddRelationshipLoadMethods()
    {
        if (!_info.HasRelationships)
            return this;
        
        foreach (var relationship in _info.Relationships)
        {
            var method = RelationshipMethodGenerator.GenerateLoadMethod(relationship, _info);
            _code.AppendLine(method);
            _code.AppendLine();
        }
        
        return this;
    }
    
    public RepositoryCodeBuilder CloseClass()
    {
        _code.AppendLine("    }"); // Close class
        _code.AppendLine("}"); // Close namespace
        return this;
    }
    
    public string Build()
    {
        return _code.ToString();
    }
}
```

### Task 3.2: Create Relationship Method Generator (3 hours)

**File**: `src/NPA.Generators/CodeGen/RelationshipMethodGenerator.cs`

```csharp
namespace NPA.Generators.CodeGen;

public static class RelationshipMethodGenerator
{
    public static string GenerateLoadMethod(RelationshipMetadata relationship, RepositoryInfo repository)
    {
        return relationship.Type switch
        {
            RelationshipType.OneToMany => GenerateOneToManyLoad(relationship, repository),
            RelationshipType.ManyToOne => GenerateManyToOneLoad(relationship, repository),
            RelationshipType.OneToOne => GenerateOneToOneLoad(relationship, repository),
            RelationshipType.ManyToMany => GenerateManyToManyLoad(relationship, repository),
            _ => string.Empty
        };
    }
    
    private static string GenerateOneToManyLoad(RelationshipMetadata rel, RepositoryInfo repo)
    {
        var methodName = $"GetByIdWith{rel.PropertyName}Async";
        
        return $@"        public async Task<{repo.EntityType}?> {methodName}({repo.KeyType} id)
        {{
            // TODO: Implement OneToMany load
            // Will be completed in next iteration
            throw new NotImplementedException();
        }}";
    }
    
    // Implement other methods similarly...
}
```

**Checklist**:
- [ ] Create `RepositoryCodeBuilder` class
- [ ] Implement builder methods
- [ ] Create `RelationshipMethodGenerator` class
- [ ] Add placeholder implementations
- [ ] Build and verify

## Week 1 Completion Checklist

- [ ] Layer 2 Models created
- [ ] MetadataExtractor enhanced
- [ ] Unit tests passing
- [ ] RepositoryGenerator extracts relationships
- [ ] Code generation infrastructure ready
- [ ] All code builds without errors
- [ ] Run existing tests to ensure nothing broken

## Week 2 Preview

Next week you'll implement:
- Complete SQL generation for relationship loading
- Implement each relationship type's load method
- Add eager loading support
- Integration tests

## Verification

Run these commands to verify Week 1 completion:

```bash
# Build the generators project
dotnet build src/NPA.Generators/NPA.Generators.csproj

# Run unit tests
dotnet test tests/NPA.Generators.Tests/NPA.Generators.Tests.csproj

# Verify no existing tests broken
dotnet test
```

Expected results:
- ✅ All projects build successfully
- ✅ All existing tests still pass
- ✅ New MetadataExtractor tests pass

## Troubleshooting

### Issue: Can't find RelationshipMetadata
**Solution**: Make sure the Models folder and namespace are correct. Rebuild the project.

### Issue: MetadataExtractor tests fail
**Solution**: Ensure your test source includes all necessary attributes and using statements.

### Issue: Existing tests broken
**Solution**: You likely changed existing code. Revert and make changes additive only.

## Next Steps

Once Week 1 is complete, proceed to Week 2 implementation in the full IMPLEMENTATION_GUIDE.md.
