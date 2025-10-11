# Phase 2.7: DI Registration Guide - Option 4 Implementation

## 🎯 Where Does DI Registration Happen?

This document explains **exactly where** the DI registration code goes for Option 4 (the fastest approach).

## 📍 Registration Locations

### 1. Central Extension Method (NEW)

**File:** `src/NPA.Core/Extensions/ServiceCollectionExtensions.cs` ⭐ **NEW FILE**

**Location:** In the NPA.Core project (the central library)

```csharp
namespace NPA.Core.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Smart registration - detects and uses generated provider if available.
    /// </summary>
    public static IServiceCollection AddNpaMetadataProvider(
        this IServiceCollection services)
    {
        // This method does the detection and registration
        var generatedType = FindGeneratedMetadataProvider();
        
        if (generatedType != null)
        {
            // FAST: Use generated implementation
            services.AddSingleton(typeof(IMetadataProvider), generatedType);
        }
        else
        {
            // FALLBACK: Use reflection-based implementation
            services.AddSingleton<IMetadataProvider, MetadataProvider>();
        }
        
        return services;
    }
    
    private static Type? FindGeneratedMetadataProvider()
    {
        // Scan assemblies for NPA.Generated.GeneratedMetadataProvider
        // ... implementation details
    }
}
```

**Why here?**
- ✅ Part of NPA.Core (no new dependencies)
- ✅ Available to all consumers
- ✅ Single source of truth
- ✅ Easy to maintain

### 2. Provider Extensions (MODIFIED)

**Files:** All 4 database provider extension files

**Files to Modify:**
- `src/NPA.Providers.PostgreSql/Extensions/ServiceCollectionExtensions.cs`
- `src/NPA.Providers.SqlServer/Extensions/ServiceCollectionExtensions.cs`
- `src/NPA.Providers.MySql/Extensions/ServiceCollectionExtensions.cs`
- `src/NPA.Providers.Sqlite/Extensions/ServiceCollectionExtensions.cs`

**Change (in each file):**
```csharp
public static IServiceCollection AddPostgreSqlProvider(
    this IServiceCollection services, 
    string connectionString)
{
    // Register dialect, type converter, bulk operations...
    services.AddSingleton<ISqlDialect, PostgreSqlDialect>();
    services.AddSingleton<ITypeConverter, PostgreSqlTypeConverter>();
    services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();
    
    // BEFORE (Line 47):
    // services.AddSingleton<IMetadataProvider, MetadataProvider>();
    
    // AFTER:
    services.AddNpaMetadataProvider();  // ← Uses smart registration!
    
    // Register entity manager...
    services.AddScoped<IEntityManager, EntityManager>();
    
    return services;
}
```

**Why modify these?**
- ✅ Convenient provider-specific setup
- ✅ Users just call `AddPostgreSqlProvider()`
- ✅ Everything configured automatically

### 3. Sample Applications (MODIFIED)

**Files:** 7 sample runner files

**Files to Modify:**
1. `samples/BasicUsage/Features/PostgreSqlProviderRunner.cs` (Line 50)
2. `samples/BasicUsage/Features/SqlServerProviderRunner.cs` (Line 45)
3. `samples/BasicUsage/Features/MySqlProviderRunner.cs` (Line 45)
4. `samples/ConsoleAppSync/Features/SyncMethodsRunner.cs` (Line 46)
5. `samples/RepositoryPattern/Program.cs` (Line 88)
6. `samples/AdvancedQueries/Program.cs` (Line 39)

**Change (in each sample):**
```csharp
var services = new ServiceCollection();

// BEFORE:
// services.AddSingleton<IMetadataProvider, MetadataProvider>();

// AFTER:
services.AddNpaMetadataProvider();

services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();
// ... rest of setup
```

**Why modify these?**
- ✅ Demonstrate best practice
- ✅ Show automatic detection works
- ✅ Educational value

## 🏗️ Architecture Flow

### At Compile Time:
```
[Entity] classes in project
    ↓
EntityMetadataGenerator (Phase 2.6)
    ↓
Generates: NPA.Generated.GeneratedMetadataProvider
           implements IMetadataProvider  ← Generated in consumer project!
```

### At Runtime (Startup):
```
Application calls: services.AddNpaMetadataProvider()
    ↓
ServiceCollectionExtensions.AddNpaMetadataProvider()
    ↓
FindGeneratedMetadataProvider() scans assemblies
    ↓
Found? → Register GeneratedMetadataProvider (FAST)
Not Found? → Register MetadataProvider (FALLBACK)
    ↓
DI Container now has IMetadataProvider
```

### At Runtime (Usage):
```
EntityManager needs metadata
    ↓
Calls: _metadataProvider.GetEntityMetadata(typeof(User))
    ↓
If GeneratedMetadataProvider:
    → Dictionary lookup (~1-2ns) ⚡ FAST
    
If MetadataProvider (fallback):
    → Reflection (~50-500ns) 🐌 SLOWER
```

## 📊 Performance by Location

| Registration Location | Performance Impact | Notes |
|----------------------|-------------------|-------|
| **Option 1: HybridMetadataProvider** | Medium | Uses `MethodInfo.Invoke()` - ~10-20ns overhead per call |
| **Option 2: Extension Helper** | Same as Option 1 | Just wraps Option 1 |
| **Option 4: Direct Implementation** | ⚡ **FASTEST** | Virtual method call - ~1-2ns |

## 🎯 Key Insight: Why Option 4 is Fastest

### Option 1/2 (Every call):
```csharp
// In HybridMetadataProvider.GetEntityMetadata():
var metadata = _getMetadataMethod.Invoke(null, new object[] { entityType });
//             ↑
//             This is REFLECTION! Has overhead:
//             - Boxing of parameters (new object[])
//             - MethodInfo dispatch
//             - Unboxing of return value
//             Cost: ~10-20ns per call
```

### Option 4 (Every call):
```csharp
// DI container resolved: IMetadataProvider provider = new GeneratedMetadataProvider()
var metadata = provider.GetEntityMetadata(entityType);
//             ↑
//             This is a VIRTUAL METHOD CALL! Very fast:
//             - Direct vtable lookup
//             - No boxing/unboxing
//             - JIT optimized
//             Cost: ~1-2ns per call (10x faster!)
```

## 📍 Summary: DI Registration Map

```
┌─────────────────────────────────────────────────────────────┐
│ NPA.Core Project (Library)                                  │
├─────────────────────────────────────────────────────────────┤
│ Extensions/                                                 │
│   └── ServiceCollectionExtensions.cs  ← Central smart      │
│       • AddNpaMetadataProvider()         registration       │
│       • FindGeneratedMetadataProvider()                     │
└─────────────────────────────────────────────────────────────┘
                            ↑
                            │ Called by
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Provider Projects (PostgreSQL, SQL Server, MySQL, SQLite)   │
├─────────────────────────────────────────────────────────────┤
│ Extensions/ServiceCollectionExtensions.cs                   │
│   • AddPostgreSqlProvider() ──→ calls AddNpaMetadataProvider()│
│   • AddSqlServerProvider()  ──→ calls AddNpaMetadataProvider()│
│   • AddMySqlProvider()      ──→ calls AddNpaMetadataProvider()│
│   • AddSqliteProvider()     ──→ calls AddNpaMetadataProvider()│
└─────────────────────────────────────────────────────────────┘
                            ↑
                            │ Called by
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Consumer Applications (Samples, User Apps)                  │
├─────────────────────────────────────────────────────────────┤
│ Program.cs or Startup.cs                                    │
│   services.AddPostgreSqlProvider(connectionString);         │
│                                                             │
│ Or manual:                                                  │
│   services.AddNpaMetadataProvider();                        │
│                                                             │
│ Generated at compile time:                                  │
│   NPA.Generated.GeneratedMetadataProvider                   │
│   (implements IMetadataProvider)                            │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Usage Examples

### Example 1: Using Provider Extension (Recommended)
```csharp
var builder = WebApplication.CreateBuilder(args);

// This internally calls AddNpaMetadataProvider()
builder.Services.AddPostgreSqlProvider(connectionString);
//                                     ↑
//                                     Automatically uses generated
//                                     metadata if available!

var app = builder.Build();
```

### Example 2: Manual Registration
```csharp
var services = new ServiceCollection();

// Explicit smart registration
services.AddNpaMetadataProvider();  // Detects generated provider
services.AddSingleton<IDatabaseProvider, PostgreSqlProvider>();
services.AddScoped<IEntityManager, EntityManager>();

var provider = services.BuildServiceProvider();
```

### Example 3: Testing Without Generator
```csharp
var services = new ServiceCollection();

// No [Entity] classes or generator → automatically uses MetadataProvider
services.AddNpaMetadataProvider();  // Falls back to reflection

// Everything still works, just slower
```

## 🎓 Key Takeaways

1. **Central registration** happens in `NPA.Core/Extensions/ServiceCollectionExtensions.cs`
2. **Provider extensions** all call this central method
3. **Smart detection** happens once at DI configuration time
4. **Zero overhead** at runtime - just normal virtual method calls
5. **Automatic fallback** if no generated provider exists

---

**Performance Answer:**
- **Option 1/2:** ~10-20ns per metadata call (MethodInfo.Invoke overhead)
- **Option 4:** ~1-2ns per metadata call (virtual method call)
- **Result:** Option 4 is **~10x faster than Option 1/2**! 🚀

