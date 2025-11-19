# Phase 8.1: Schema Generation from Entities

## Overview
Generate database schema (DDL) from entity definitions automatically. This phase creates the foundation for the migration system by converting entity metadata into CREATE TABLE, CREATE INDEX, and constraint statements.

## Objectives
- Generate CREATE TABLE statements from entity classes
- Support all column types, constraints, and attributes
- Generate foreign keys from relationship mappings
- Create indexes based on entity configuration
- Support multiple database providers (SQL Server, PostgreSQL, MySQL)

## Tasks

### 1. Schema Model Creation
- [ ] Create `SchemaModel` to represent database schema
- [ ] Create `TableDefinition` model with columns, constraints
- [ ] Create `ColumnDefinition` with type, nullable, default value
- [ ] Create `ForeignKeyDefinition` for relationships
- [ ] Create `IndexDefinition` for performance optimization

### 2. Entity to Schema Converter
- [ ] Implement `EntitySchemaConverter` to convert entities to schema
- [ ] Map C# types to SQL types for each provider
- [ ] Extract column definitions from properties
- [ ] Generate primary key constraints
- [ ] Generate unique constraints
- [ ] Generate check constraints

### 3. Relationship to Foreign Key Converter
- [ ] Convert `ManyToOne` relationships to foreign keys
- [ ] Convert `OneToOne` relationships to foreign keys
- [ ] Handle `JoinColumn` configuration
- [ ] Support nullable vs required foreign keys
- [ ] Generate relationship constraints (ON DELETE, ON UPDATE)

### 4. Index Generation
- [ ] Generate indexes for foreign key columns
- [ ] Support `[Index]` attribute configuration
- [ ] Generate composite indexes
- [ ] Support unique indexes
- [ ] Support filtered indexes (WHERE clause)

### 5. Database Provider Abstraction
- [ ] Create `ISchemaGenerator` interface
- [ ] Implement `SqlServerSchemaGenerator`
- [ ] Implement `PostgreSqlSchemaGenerator`
- [ ] Implement `MySqlSchemaGenerator`
- [ ] Support provider-specific syntax and types

## Example Usage

```csharp
[Entity]
[Table("users", Schema = "public")]
[Index("idx_users_email", nameof(Email), IsUnique = true)]
public class User
{
    [Id]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("username", Length = 50)]
    [Required]
    public string Username { get; set; }
    
    [Column("email", Length = 100)]
    [Required]
    public string Email { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    [OneToMany(MappedBy = "User")]
    public ICollection<Order> Orders { get; set; }
}

// Generated SQL Server schema:
CREATE TABLE [public].[users] (
    [id] INT NOT NULL IDENTITY(1,1),
    [username] NVARCHAR(50) NOT NULL,
    [email] NVARCHAR(100) NOT NULL,
    [created_at] DATETIME2 NOT NULL,
    [is_active] BIT NOT NULL DEFAULT 1,
    CONSTRAINT [PK_users] PRIMARY KEY ([id])
);

CREATE UNIQUE INDEX [idx_users_email] ON [public].[users] ([email]);

// Generated PostgreSQL schema:
CREATE TABLE public.users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL,
    created_at TIMESTAMP NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    CONSTRAINT pk_users PRIMARY KEY (id)
);

CREATE UNIQUE INDEX idx_users_email ON public.users (email);
```

## Schema Model Structure

```csharp
public class SchemaModel
{
    public List<TableDefinition> Tables { get; set; }
    public List<ForeignKeyDefinition> ForeignKeys { get; set; }
    public List<IndexDefinition> Indexes { get; set; }
}

public class TableDefinition
{
    public string Name { get; set; }
    public string? Schema { get; set; }
    public List<ColumnDefinition> Columns { get; set; }
    public PrimaryKeyDefinition PrimaryKey { get; set; }
    public List<UniqueConstraintDefinition> UniqueConstraints { get; set; }
    public List<CheckConstraintDefinition> CheckConstraints { get; set; }
}

public class ColumnDefinition
{
    public string Name { get; set; }
    public string DataType { get; set; }
    public bool IsNullable { get; set; }
    public string? DefaultValue { get; set; }
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsIdentity { get; set; }
}

public class ForeignKeyDefinition
{
    public string Name { get; set; }
    public string TableName { get; set; }
    public string[] Columns { get; set; }
    public string ReferencedTableName { get; set; }
    public string[] ReferencedColumns { get; set; }
    public ForeignKeyAction OnDelete { get; set; }
    public ForeignKeyAction OnUpdate { get; set; }
}

public enum ForeignKeyAction
{
    NoAction,
    Cascade,
    SetNull,
    SetDefault,
    Restrict
}
```

## Type Mapping Examples

```csharp
public static class TypeMapper
{
    // SQL Server mappings
    private static readonly Dictionary<Type, string> SqlServerTypes = new()
    {
        [typeof(int)] = "INT",
        [typeof(long)] = "BIGINT",
        [typeof(string)] = "NVARCHAR",
        [typeof(bool)] = "BIT",
        [typeof(DateTime)] = "DATETIME2",
        [typeof(decimal)] = "DECIMAL",
        [typeof(Guid)] = "UNIQUEIDENTIFIER",
        [typeof(byte[])] = "VARBINARY"
    };
    
    // PostgreSQL mappings
    private static readonly Dictionary<Type, string> PostgreSqlTypes = new()
    {
        [typeof(int)] = "INTEGER",
        [typeof(long)] = "BIGINT",
        [typeof(string)] = "VARCHAR",
        [typeof(bool)] = "BOOLEAN",
        [typeof(DateTime)] = "TIMESTAMP",
        [typeof(decimal)] = "NUMERIC",
        [typeof(Guid)] = "UUID",
        [typeof(byte[])] = "BYTEA"
    };
}
```

## Acceptance Criteria
- [ ] Entity metadata correctly converted to schema model
- [ ] All column types properly mapped for each provider
- [ ] Primary keys generated correctly
- [ ] Foreign keys created from relationships
- [ ] Indexes generated for foreign keys and attributes
- [ ] Provider-specific syntax handled correctly
- [ ] Support for composite keys
- [ ] Support for nullable and required fields
- [ ] Default values properly generated

## Dependencies
- Phase 2.1: Relationship Mapping
- Phase 2.6: Metadata Source Generator
- Existing: NPA.Core.Metadata

## Testing Requirements
- Unit tests for type mapping
- Unit tests for schema conversion
- Integration tests for each database provider
- Tests for complex entity scenarios
- Tests for relationship foreign keys
- Validation tests for schema model

## Documentation
- Type mapping reference for each provider
- Schema generation configuration guide
- Attribute reference for schema customization
- Examples for common scenarios
- Provider-specific considerations
