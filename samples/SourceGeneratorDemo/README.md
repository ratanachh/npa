# Source Generator Demo

This sample demonstrates NPA's powerful source generators that generate code at compile time.

## 🎯 What This Sample Demonstrates

### 1. Repository Generator (Phase 1.6) ✅
- Automatic repository implementation from interfaces
- Convention-based method generation
- Type-safe CRUD operations without boilerplate code

### 2. Metadata Generator (Phase 2.6) ✅ NEW
- Compile-time entity metadata generation
- Zero runtime reflection (10-100x performance improvement)
- Automatic entity discovery and metadata pre-computation

## 🚀 How to Run

```bash
cd samples/SourceGeneratorDemo
dotnet build
dotnet run
```

## 📋 What Gets Generated

### Repository Implementation
When you mark an interface with `[Repository(typeof(Entity))]`:

```csharp
[Repository(typeof(User))]
public interface IUserRepository
{
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> FindByUsernameAsync(string username);
}
```

The generator creates:

```csharp
public partial class UserRepository : IUserRepository
{
    private readonly IEntityManager _entityManager;
    
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var sql = "SELECT * FROM users";
        return await _connection.QueryAsync<User>(sql);
    }
    
    public async Task<User?> GetByIdAsync(int id)
    {
        var sql = "SELECT * FROM users WHERE id = @id";
        return await _connection.QueryFirstOrDefaultAsync<User>(sql, new { id });
    }
    
    // ... more generated methods
}
```

### Entity Metadata
When you mark a class with `[Entity]`:

```csharp
[Entity]
[Table("users")]
public class User
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    public int Id { get; set; }
    
    [Column("username")]
    public string Username { get; set; } = string.Empty;
}
```

The generator creates:

```csharp
namespace NPA.Generated;

public static class GeneratedMetadataProvider
{
    private static readonly Dictionary<Type, EntityMetadata> _metadata = new()
    {
        { typeof(User), UserMetadata() },
    };
    
    public static EntityMetadata? GetMetadata(Type entityType)
    {
        _metadata.TryGetValue(entityType, out var metadata);
        return metadata;
    }
    
    private static EntityMetadata UserMetadata()
    {
        return new EntityMetadata
        {
            EntityType = typeof(User),
            TableName = "users",
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                { "Id", new PropertyMetadata
                {
                    PropertyName = "Id",
                    ColumnName = "id",
                    PropertyType = typeof(int),
                    IsPrimaryKey = true,
                    GenerationType = GenerationType.Identity,
                    // ... more properties
                } },
                // ... more properties
            }
        };
    }
}
```

## 🔍 Viewing Generated Code

After building, you can find the generated code in:

```
obj/Debug/net8.0/generated/
├── NPA.Generators.RepositoryGenerator/
│   └── UserRepositoryImplementation.g.cs
└── NPA.Generators.EntityMetadataGenerator/
    └── GeneratedMetadataProvider.g.cs
```

Or check the `obj/Debug/net8.0/NPA.Generators/` directory.

## 📊 Performance Benefits

### Repository Generator
- ✅ **Zero reflection** - All code generated at compile time
- ✅ **Type-safe** - Compiler validates all generated code
- ✅ **Optimized** - Direct Dapper calls, no indirection
- ✅ **IntelliSense** - Full IDE support

### Metadata Generator  
- ✅ **10-100x faster** - No runtime attribute scanning
- ✅ **O(1) lookup** - Dictionary-based metadata access
- ✅ **No GC pressure** - Static readonly, no allocations
- ✅ **Predictable** - Consistent performance

## 🎓 Key Concepts

### Convention-Based Generation
The repository generator follows naming conventions:

| Method Pattern | Generated SQL |
|----------------|---------------|
| `GetAllAsync()` | `SELECT * FROM table` |
| `GetByIdAsync(id)` | `SELECT * FROM table WHERE id = @id` |
| `FindBy{Property}Async(value)` | `SELECT * FROM table WHERE {property} = @value` |
| `SaveAsync(entity)` | `EntityManager.PersistAsync(entity)` |
| `UpdateAsync(entity)` | `EntityManager.MergeAsync(entity)` |
| `DeleteAsync(id)` | `EntityManager.RemoveAsync<T>(id)` |
| `CountAsync()` | `SELECT COUNT(*) FROM table` |

### Incremental Generation
Both generators use `IIncrementalGenerator` for:
- ✅ Only regenerating code when sources change
- ✅ Caching intermediate results
- ✅ Faster incremental builds
- ✅ Better IDE performance

## 💡 Best Practices

1. **Use Interfaces for Repositories**: Let the generator create implementations
2. **Follow Naming Conventions**: Get automatic method implementations
3. **Mark All Entities**: Ensure metadata is generated for all entities
4. **Build to Generate**: Code is generated during compilation
5. **Check Generated Code**: Review generated files for learning

## 🔗 Related Documentation

- [Phase 1.6: Repository Generator](../../docs/tasks/phase1.6-repository-source-generator-basic/README.md)
- [Phase 2.6: Metadata Generator](../../docs/tasks/phase2.6-metadata-source-generator/README.md)
- [Getting Started Guide](../../docs/GettingStarted.md)

## 📦 Dependencies

- NPA.Core (entity framework)
- NPA.Generators (source generators)
- .NET 8.0 SDK

## 🎯 Learning Outcomes

After running this sample, you'll understand:
- ✅ How source generators work in NPA
- ✅ Convention-based method generation
- ✅ Compile-time metadata generation
- ✅ Performance benefits of zero-reflection approach
- ✅ How to view and debug generated code

---

**Status:** ✅ Complete (Phase 1.6 & 2.6)  
**Last Updated:** October 11, 2025

