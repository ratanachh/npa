# NPA Sample Projects

This directory contains sample applications demonstrating NPA features.

## 📊 Current Status

| Sample | Status | Functional? | Phase | Description |
|--------|--------|-------------|-------|-------------|
| **BasicUsage** | ⚠️ Needs Update | Yes* | 1.1-1.3 | Entity mapping, CRUD, queries |
| **AdvancedQueries** | 📋 Stub | No | 2.3+ | Placeholder for advanced queries |
| **RepositoryPattern** | 📋 Stub | No | 2.4 | Placeholder for repository pattern |
| **SourceGeneratorDemo** | 📋 Stub | No | 4.1+ | Placeholder for source generators |
| **WebApplication** | 📋 Stub | No | 6.1 | Placeholder for ASP.NET Core |

\* BasicUsage is functional but needs to be updated to use PostgreSQL instead of SQL Server

## ✅ Currently Functional

### BasicUsage
Demonstrates Phase 1.1-1.3 features:
- Entity mapping with attributes
- EntityManager CRUD operations
- CPQL query language
- SQL Server provider (⚠️ in progress)

**Known Issue**: Should use PostgreSQL provider (completed) instead of SQL Server (in progress)

**To Run**:
```bash
cd BasicUsage
dotnet run

# Or with PostgreSQL (recommended after fixing Program.cs):
dotnet run -- postgresql
```

**Fix Needed**:
In `Program.cs` line 9, change default to:
```csharp
string provider = args.Length > 0 ? args[0].ToLowerInvariant() : "postgresql";
```

## 📋 Placeholder Samples

The following samples are **not functional** and serve as placeholders for future features:

- **AdvancedQueries**: Depends on Phase 2.3 (JPQL) - not implemented
- **RepositoryPattern**: Depends on Phase 2.4 - not implemented  
- **SourceGeneratorDemo**: Depends on Phase 4.1 - not implemented
- **WebApplication**: Depends on Phase 6.1 - not implemented

These contain TODO comments and interface definitions as design references.

## 🎯 Creating New Samples

For detailed instructions on creating samples that match current implementation:

1. See task documents: `docs/tasks/samples/`
2. Follow these ready-to-implement guides:
   - [Phase 1.1 - Basic Entity Mapping](../docs/tasks/samples/phase1.1-basic-entity-mapping-sample.md)
   - [Phase 1.2 - CRUD Operations](../docs/tasks/samples/phase1.2-crud-operations-sample.md)
   - [Phase 1.3 - CPQL Queries](../docs/tasks/samples/phase1.3-cpql-query-sample.md)

## 📚 Documentation

- **Sample Status**: `docs/tasks/samples/EXISTING-SAMPLES-STATUS.md`
- **Sample Index**: `docs/tasks/samples/SAMPLES-INDEX.md`
- **Main README**: `docs/tasks/samples/README.md`

## 🔧 Quick Actions

### Update BasicUsage to PostgreSQL
```bash
# Edit samples/BasicUsage/Program.cs
# Change line 9 from "sqlserver" to "postgresql"
string provider = args.Length > 0 ? args[0].ToLowerInvariant() : "postgresql";

# Run the sample
dotnet run --project samples/BasicUsage
```

### Create New Phase 1 Samples
```bash
# Follow the task documents to create focused samples
# Each demonstrates a specific phase feature
# Uses PostgreSQL (the completed provider)
```

## 💡 Recommendations

1. **Use BasicUsage** as reference but update it for PostgreSQL
2. **Ignore stub samples** until their required phases are implemented
3. **Follow task documents** in `docs/tasks/samples/` for accurate guidance
4. **Use PostgreSQL** for all Phase 1 samples (it's completed and tested)

---

*Last Updated: October 8, 2025*  
*See docs/tasks/samples/EXISTING-SAMPLES-STATUS.md for detailed status*
