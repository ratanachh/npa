# NPA Sample Projects

This directory contains sample applications demonstrating NPA features.

## 📊 Current Status

| Sample | Status | Functional? | Phase | Description |
|--------|--------|-------------|-------|-------------|
| **BasicUsage** | ✅ Complete | Yes | 1.1-1.4 | Entity mapping, CRUD, queries with SQL Server |
| **AdvancedQueries** | ✅ Complete | Yes | 1.3 | Advanced CPQL queries with PostgreSQL |
| **RepositoryPattern** | ✅ Builds | Partial | 2.4 | Repository pattern (needs full implementation) |
| **SourceGeneratorDemo** | 🚧 Stub | No | 1.6/4.1 | Placeholder (requires generators) |
| **WebApplication** | ✅ Builds | Partial | 2.4 | ASP.NET Core integration (basic) |

## ✅ Fully Functional Samples

### BasicUsage (Phases 1.1-1.4)
Demonstrates complete Phase 1 features:
- ✅ Entity mapping with attributes (Phase 1.1)
- ✅ EntityManager CRUD operations (Phase 1.2)
- ✅ CPQL query language (Phase 1.3)
- ✅ SQL Server provider (Phase 1.4 - 63 tests passing)
- ✅ PostgreSQL provider (alternative)

**To Run**:
```bash
cd BasicUsage
dotnet run                    # Uses SQL Server (default)
dotnet run -- postgresql      # Uses PostgreSQL (alternative)
```

### AdvancedQueries (Phase 1.3)
Demonstrates advanced CPQL query capabilities:
- ✅ Complex WHERE conditions with AND/OR
- ✅ Range queries (BETWEEN equivalent)
- ✅ Pattern matching (LIKE queries)
- ✅ DateTime queries
- ✅ NULL handling
- ✅ COUNT aggregations
- ✅ Bulk UPDATE operations
- ✅ Multiple parameter binding

**To Run**:
```bash
cd AdvancedQueries
dotnet run                    # Uses PostgreSQL with Testcontainers
```

## 🚧 Partial/Placeholder Samples

### RepositoryPattern
- ✅ Builds successfully
- 🚧 Partial implementation - needs Phase 2.4 features

### WebApplication
- ✅ Builds successfully
- 🚧 Basic ASP.NET Core integration
- Needs full implementation for production use

### SourceGeneratorDemo
- 🚧 Stub only - depends on Phase 1.6/4.1
- Cannot build until NPA.Generators is implemented

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

1. **Use BasicUsage** as the primary reference for Phases 1.1-1.4
2. **Use AdvancedQueries** to learn CPQL query capabilities
3. **SQL Server provider** is production-ready with 63 passing tests
4. **PostgreSQL provider** is available as an alternative
5. **Follow task documents** in `docs/tasks/` for phase-specific guidance

---

*Last Updated: October 9, 2025*  
*Status: 2 fully functional samples demonstrating Phases 1.1-1.4*
