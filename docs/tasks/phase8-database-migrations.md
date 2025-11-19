# Phase 8: Database Migrations

## Overview
Comprehensive database migration system that generates database schemas from entity definitions, creates versioned migration files, and provides tools for managing schema evolution over time. Similar to Entity Framework migrations but integrated with NPA's source generation architecture.

## Vision
Enable developers to manage database schema changes through code, with automatic generation from entity definitions, version control for schema evolution, and safe migration execution with rollback capabilities.

## Benefits
- **Code-First Development**: Define schema through entity classes
- **Version Control**: Track all schema changes in source control
- **Automated Generation**: Auto-generate migrations from entity changes
- **Safe Execution**: Transaction support with rollback capabilities
- **Multi-Database Support**: Works with SQL Server, PostgreSQL, MySQL
- **Team Collaboration**: Avoid schema conflicts in team environments
- **CI/CD Integration**: Automated migration execution in deployment pipelines
- **Schema Validation**: Detect and fix schema drift automatically

## Design Principles
1. **Entity-Driven**: Schema derives from entity definitions and attributes
2. **Version-Based**: Each migration has unique timestamp-based ID
3. **Idempotent**: Migrations can be safely re-run
4. **Transactional**: Changes are atomic with rollback support
5. **Auditable**: Complete history of all schema changes
6. **Provider-Agnostic**: Abstract SQL generation per database provider
7. **Developer-Friendly**: Intuitive CLI and clear error messages
8. **Production-Safe**: Locking mechanism prevents concurrent migrations

## Architecture

### High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         NPA Migration System                         │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌────────────────┐      ┌─────────────────┐      ┌──────────────┐ │
│  │   Entity       │      │    Schema       │      │  Migration   │ │
│  │  Definitions   │─────▶│   Generator     │─────▶│  Generator   │ │
│  │  + Attributes  │      │                 │      │              │ │
│  └────────────────┘      └─────────────────┘      └──────────────┘ │
│         │                        │                        │          │
│         │                        │                        ▼          │
│         │                        │                ┌──────────────┐  │
│         │                        │                │  Migration   │  │
│         │                        │                │    Files     │  │
│         │                        │                │  (.cs/SQL)   │  │
│         │                        │                └──────────────┘  │
│         │                        │                        │          │
│         ▼                        ▼                        ▼          │
│  ┌────────────────┐      ┌─────────────────┐      ┌──────────────┐ │
│  │    Schema      │      │    Schema       │      │  Migration   │ │
│  │   Comparer     │◀─────│    Reader       │      │   Executor   │ │
│  │                │      │                 │      │              │ │
│  └────────────────┘      └─────────────────┘      └──────────────┘ │
│         │                                                  │          │
│         │                                                  ▼          │
│         ▼                                          ┌──────────────┐  │
│  ┌────────────────┐                               │  Migration   │  │
│  │ Corrective     │                               │   History    │  │
│  │  Migration     │                               │  Repository  │  │
│  │  Generation    │                               └──────────────┘  │
│  └────────────────┘                                       │          │
│         │                                                  ▼          │
│         ▼                                          ┌──────────────┐  │
│  ┌────────────────────────────────────────┐       │   Database   │  │
│  │           CLI Tools                    │       │              │  │
│  │  • migration add/list/status           │       └──────────────┘  │
│  │  • migrate up/down                     │                          │
│  │  • database seed/validate              │                          │
│  │  • schema diff/sync                    │                          │
│  └────────────────────────────────────────┘                          │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

### Component Flow

```
1. Development Flow:
   Entity Definition → Schema Generation → Migration File → Source Control

2. Deployment Flow:
   Migration Files → Migration Executor → Database Update → History Tracking

3. Validation Flow:
   Database Schema → Schema Reader → Schema Comparer → Drift Report

4. Sync Flow:
   Schema Drift → Corrective Migration → Auto-fix → Validation
```

## Phase Structure

### Phase 8.1: Schema Generation from Entities (2 weeks)
Generate database schema definitions from entity classes and their attributes.

**Key Features:**
- Schema models (TableDefinition, ColumnDefinition, ForeignKeyDefinition)
- Entity-to-schema converter
- Type mapping for different database providers
- Relationship-to-foreign-key conversion
- Index generation from attributes
- Constraint generation

**Deliverables:**
- `ISchemaGenerator` interface and implementations
- Schema models and definitions
- Type mapping tables for SQL Server, PostgreSQL, MySQL
- Unit tests for schema generation

### Phase 8.2: Migration Generation (2 weeks)
Create versioned migration files from schema changes or manual definitions.

**Key Features:**
- Migration class with Up/Down methods
- Timestamp-based migration naming
- Schema comparison to detect changes
- MigrationBuilder API for fluent syntax
- Migration history table (__MigrationsHistory)
- Support for both C# and raw SQL migrations

**Deliverables:**
- `Migration` base class
- `MigrationBuilder` fluent API
- `MigrationGenerator` for creating migration files
- Schema comparison algorithm
- Migration scaffolding templates

### Phase 8.3: Migration Execution Engine (2 weeks)
Execute migrations with transaction management and rollback support.

**Key Features:**
- Migration executor with transaction modes
- Database lock mechanism (prevent concurrent migrations)
- Migration validation (checksum verification)
- Up/Down execution with rollback
- Progress reporting and logging
- Error handling and recovery

**Deliverables:**
- `MigrationExecutor` implementation
- `IMigrationLock` implementations per provider
- `MigrationHistoryRepository` for tracking
- Transaction management
- Integration with existing provider infrastructure

### Phase 8.4: Schema Comparison and Diff (2 weeks)
Compare database schema with entity definitions to detect drift.

**Key Features:**
- Database schema reader
- Schema comparison engine
- Diff report generation (text and JSON)
- Risk assessment (breaking vs non-breaking changes)
- Corrective migration generation
- Schema validation

**Deliverables:**
- `IDatabaseSchemaReader` implementations
- `SchemaComparer` engine
- Diff report formatters
- Schema validation tools
- Automated fix suggestions

### Phase 8.5: Data Migration Support (2 weeks)
Enable data transformations and seed data management.

**Key Features:**
- Raw SQL execution in migrations
- Batch data processing for large datasets
- Seed data definitions and management
- Data validation pre/post migration
- Idempotent seed data insertion
- Environment-specific seed data

**Deliverables:**
- `DataMigration` base class
- Seed data configuration API
- Data validation framework
- Batch processing utilities
- Data migration examples and templates

### Phase 8.6: Migration CLI Tools (2 weeks)
Comprehensive command-line interface for all migration operations.

**Key Features:**
- Core commands (add, list, status, up, down, remove, script)
- Database commands (create, drop, seed, validate)
- Schema commands (diff, sync)
- Interactive prompts and confirmations
- Rich console output with colors and progress bars
- JSON output for automation
- Configuration file support

**Deliverables:**
- Full CLI implementation using System.CommandLine
- Rich console output using Spectre.Console
- Configuration management
- Comprehensive help documentation
- CI/CD integration examples

## Implementation Timeline

### Weeks 1-2: Phase 8.1 (Schema Generation)
- Week 1: Schema models, entity-to-schema converter
- Week 2: Type mapping, provider-specific generators

### Weeks 3-4: Phase 8.2 (Migration Generation)
- Week 1: Migration classes, MigrationBuilder API
- Week 2: Schema comparison, migration scaffolding

### Weeks 5-6: Phase 8.3 (Migration Execution)
- Week 1: Executor, transaction management
- Week 2: Locking, history tracking, rollback

### Weeks 7-8: Phase 8.4 (Schema Comparison)
- Week 1: Schema reader, comparison engine
- Week 2: Diff reports, validation, corrective migrations

### Weeks 9-10: Phase 8.5 (Data Migration)
- Week 1: Data migration operations, batch processing
- Week 2: Seed data, validation, complex scenarios

### Weeks 11-12: Phase 8.6 (CLI Tools)
- Week 1: Core CLI commands, database commands
- Week 2: Schema commands, interactive features, configuration

**Total Duration:** 12 weeks

## Success Metrics

### Technical Metrics
- ✅ Generate accurate schema from all entity types
- ✅ Support SQL Server, PostgreSQL, MySQL providers
- ✅ Execute migrations with <1% failure rate
- ✅ Handle schema drift automatically
- ✅ Process large migrations (100k+ rows) efficiently
- ✅ Zero data loss during migrations
- ✅ 100% rollback success rate
- ✅ CLI commands < 2s response time (except long operations)

### Quality Metrics
- ✅ 90%+ unit test coverage
- ✅ Integration tests with real databases
- ✅ Performance tests with large schemas
- ✅ Cross-platform compatibility (Windows, Linux, macOS)
- ✅ Comprehensive documentation
- ✅ Sample projects and examples

### User Experience Metrics
- ✅ Intuitive CLI commands
- ✅ Clear error messages with solutions
- ✅ Interactive confirmations for dangerous operations
- ✅ Rich progress feedback
- ✅ Developer-friendly documentation
- ✅ Easy CI/CD integration

## Example Workflow

### 1. Initial Setup
```bash
# Create initial migration from entities
dotnet npa migration add InitialCreate

# Review generated migration
# File: Migrations/20251119160000_InitialCreate.cs

# Apply migration
dotnet npa migrate up
```

### 2. Add New Entity
```csharp
[Table("orders")]
public class Order
{
    [PrimaryKey]
    public int Id { get; set; }
    
    [ForeignKey("users")]
    public int UserId { get; set; }
    
    [Column(TypeName = "DECIMAL(18,2)")]
    public decimal Total { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
```

```bash
# Generate migration for new entity
dotnet npa migration add AddOrders

# Preview changes
dotnet npa migrate up --dry-run

# Apply migration
dotnet npa migrate up
```

### 3. Modify Existing Entity
```csharp
// Add new property to User entity
public class User
{
    // ... existing properties
    
    [Column(TypeName = "NVARCHAR(100)")]
    public string? PhoneNumber { get; set; }  // Added
    
    [Index]
    public string Email { get; set; }  // Added index
}
```

```bash
# Compare schema
dotnet npa schema diff

# Generate migration
dotnet npa migration add AddUserPhoneAndEmailIndex

# Apply
dotnet npa migrate up
```

### 4. Production Deployment
```bash
# Check migration status
dotnet npa migration status

# Script out migrations for DBA review
dotnet npa migration script --output migration.sql

# Apply in production with validation
dotnet npa migrate up --validate-data
```

## Integration Points

### With Phase 7 (Relationship Management)
- Foreign key generation from relationship attributes
- Navigation property mapping
- Cascade delete configuration
- Index generation for foreign keys

### With Existing Providers
- Leverage existing connection management
- Use provider-specific SQL generation
- Integrate with transaction handling
- Utilize connection pooling

### With Source Generators
- Potential future: auto-generate migrations on build
- Detect entity changes and suggest migrations
- Validate entity-schema consistency at compile time

## Risk Management

### Technical Risks
| Risk | Impact | Mitigation |
|------|--------|------------|
| Data loss during migration | High | Transaction support, backup validation, dry-run mode |
| Schema drift in production | Medium | Automated detection, validation on startup, alerts |
| Concurrent migration execution | High | Database-level locks, migration history tracking |
| Type mapping inconsistencies | Medium | Comprehensive type mapping tables, validation tests |
| Performance with large schemas | Medium | Batch processing, progress tracking, optimized queries |

### Process Risks
| Risk | Impact | Mitigation |
|------|--------|------------|
| Team merge conflicts | Medium | Clear migration naming, documentation, PR reviews |
| Missing migrations in deployment | High | CI/CD validation, deployment checklists |
| Breaking changes in production | High | Dry-run mode, DBA review, staged rollout |

## Dependencies

### External Dependencies
- `System.CommandLine` (≥2.0) - CLI framework
- `Spectre.Console` (≥0.47) - Rich console output
- `Microsoft.Extensions.Configuration` - Configuration management

### Internal Dependencies
- Phase 1: Entity mapping and attributes
- Phase 2: Relationship mapping
- Phase 4: Database providers (SQL Server, PostgreSQL, MySQL)
- Phase 7: Advanced relationship features

## Testing Strategy

### Unit Tests
- Schema generation from entities
- Type mapping for all providers
- Migration builder API
- Schema comparison logic
- CLI command parsing

### Integration Tests
- End-to-end migration execution
- Rollback scenarios
- Schema drift detection
- Data migration with real data
- Multi-database provider testing

### Performance Tests
- Large schema generation (1000+ tables)
- Bulk data migration (1M+ rows)
- Schema comparison performance
- Concurrent operation handling

## Documentation Requirements

### Developer Documentation
- Getting started guide
- Migration creation tutorial
- CLI command reference
- Configuration guide
- Best practices
- Troubleshooting guide

### API Documentation
- Schema generation API
- Migration builder API
- Executor configuration
- Provider-specific options

### Examples
- Basic CRUD entity migrations
- Relationship migrations
- Data migration scenarios
- Complex schema changes
- CI/CD integration examples

## Future Enhancements (Post-Phase 8)

### Advanced Features
- Visual migration designer
- Migration dependency graph
- Automatic migration generation on build
- Blue-green deployment support
- Multi-tenant migration strategies
- Schema version branching
- Migration templates and presets

### Tooling
- VS Code extension for migration management
- Visual Studio integration
- Database diagram generation
- Migration history visualization
- Performance profiling for migrations

### Enterprise Features
- Migration approval workflows
- Audit logging and compliance
- Multi-environment orchestration
- Rollback scheduling
- Health monitoring and alerts

## Getting Started

After Phase 8 completion, developers can:

1. **Define entities** with NPA attributes
2. **Generate migrations** automatically or manually
3. **Review and customize** migration code
4. **Test migrations** with dry-run mode
5. **Apply migrations** to development/staging/production
6. **Monitor schema** for drift and issues
7. **Rollback** if needed with confidence

The migration system will provide a complete, production-ready solution for managing database schemas throughout the application lifecycle.

---

**Phase 8 Start Date:** TBD  
**Phase 8 Target Completion:** 12 weeks from start  
**Owner:** Development Team  
**Status:** Planning Complete ✅
