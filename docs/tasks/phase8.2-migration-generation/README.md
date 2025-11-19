# Phase 8.2: Migration Generation

## Overview
Generate migration files that represent schema changes between entity versions. Track schema history and create versioned migration files that can be applied to databases.

## Objectives
- Generate migration classes with Up/Down methods
- Track migration history in database
- Support incremental schema changes
- Generate idempotent migrations
- Version control friendly migration files

## Tasks

### 1. Migration Model
- [ ] Create `Migration` base class with Up/Down methods
- [ ] Create `MigrationInfo` with version, timestamp, description
- [ ] Create `MigrationOperation` classes (CreateTable, AlterTable, etc.)
- [ ] Support operation ordering and dependencies
- [ ] Implement migration serialization

### 2. Migration Generator
- [ ] Implement `MigrationGenerator` to create migration files
- [ ] Generate timestamped migration filenames
- [ ] Create C# migration class files
- [ ] Generate SQL scripts as alternative
- [ ] Support custom migration templates

### 3. Schema Comparison
- [ ] Implement `SchemaComparer` to detect changes
- [ ] Detect new tables
- [ ] Detect dropped tables
- [ ] Detect column additions
- [ ] Detect column modifications
- [ ] Detect column deletions
- [ ] Detect index changes
- [ ] Detect foreign key changes

### 4. Migration History Tracking
- [ ] Create `__MigrationsHistory` table
- [ ] Track applied migrations
- [ ] Store migration checksum for validation
- [ ] Support migration rollback tracking
- [ ] Query migration status

### 5. Migration Operations
- [ ] `CreateTableOperation`
- [ ] `DropTableOperation`
- [ ] `AddColumnOperation`
- [ ] `AlterColumnOperation`
- [ ] `DropColumnOperation`
- [ ] `RenameColumnOperation`
- [ ] `AddForeignKeyOperation`
- [ ] `DropForeignKeyOperation`
- [ ] `CreateIndexOperation`
- [ ] `DropIndexOperation`

## Example Usage

### Entity Evolution
```csharp
// Version 1: Initial entity
[Entity]
[Table("products")]
public class Product
{
    [Id]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("price")]
    public decimal Price { get; set; }
}

// Version 2: Add description and category
[Entity]
[Table("products")]
public class Product
{
    [Id]
    public int Id { get; set; }
    
    [Column("name")]
    public string Name { get; set; }
    
    [Column("price")]
    public decimal Price { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }  // NEW
    
    [ManyToOne]
    [JoinColumn("category_id")]
    public Category Category { get; set; }  // NEW
}
```

### Generated Migration

```csharp
// File: Migrations/20251119143022_AddProductDescriptionAndCategory.cs
using NPA.Migrations;

namespace YourApp.Migrations
{
    [Migration(20251119143022)]
    public class AddProductDescriptionAndCategory : Migration
    {
        public override void Up(MigrationBuilder builder)
        {
            builder.AddColumn(
                name: "description",
                table: "products",
                type: "NVARCHAR(MAX)",
                nullable: true);
            
            builder.AddColumn(
                name: "category_id",
                table: "products",
                type: "INT",
                nullable: false);
            
            builder.AddForeignKey(
                name: "FK_products_category_id",
                table: "products",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
            
            builder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");
        }
        
        public override void Down(MigrationBuilder builder)
        {
            builder.DropIndex(
                name: "IX_products_category_id",
                table: "products");
            
            builder.DropForeignKey(
                name: "FK_products_category_id",
                table: "products");
            
            builder.DropColumn(
                name: "category_id",
                table: "products");
            
            builder.DropColumn(
                name: "description",
                table: "products");
        }
    }
}
```

### CLI Usage

```bash
# Generate a new migration
dotnet npa migration add AddProductDescriptionAndCategory

# Generated output:
# ✓ Analyzing entity changes...
# ✓ Detecting schema differences...
# ✓ Generated migration: 20251119143022_AddProductDescriptionAndCategory.cs
# 
# Changes detected:
#   - Add column 'description' to 'products'
#   - Add column 'category_id' to 'products'
#   - Add foreign key 'FK_products_category_id'
#   - Add index 'IX_products_category_id'

# Apply migrations
dotnet npa migration apply

# Rollback last migration
dotnet npa migration rollback

# List migrations
dotnet npa migration list
```

## Migration Builder API

```csharp
public class MigrationBuilder
{
    // Table operations
    public void CreateTable(string name, Action<TableBuilder> buildAction);
    public void DropTable(string name);
    public void RenameTable(string oldName, string newName);
    
    // Column operations
    public void AddColumn(string name, string table, string type, bool nullable = false);
    public void AlterColumn(string name, string table, string newType, bool nullable = false);
    public void DropColumn(string name, string table);
    public void RenameColumn(string oldName, string newName, string table);
    
    // Constraint operations
    public void AddPrimaryKey(string name, string table, params string[] columns);
    public void DropPrimaryKey(string name, string table);
    public void AddUniqueConstraint(string name, string table, params string[] columns);
    public void DropUniqueConstraint(string name, string table);
    
    // Foreign key operations
    public void AddForeignKey(string name, string table, string column, 
        string principalTable, string principalColumn, 
        ReferentialAction onDelete = ReferentialAction.NoAction);
    public void DropForeignKey(string name, string table);
    
    // Index operations
    public void CreateIndex(string name, string table, params string[] columns);
    public void CreateIndex(string name, string table, bool unique, params string[] columns);
    public void DropIndex(string name, string table);
    
    // SQL execution
    public void Sql(string sql);
}
```

## Migration History Table

```sql
CREATE TABLE __MigrationsHistory (
    MigrationId NVARCHAR(150) NOT NULL PRIMARY KEY,
    ProductVersion NVARCHAR(32) NOT NULL,
    AppliedDate DATETIME2 NOT NULL,
    Checksum NVARCHAR(64) NOT NULL
);

-- Example data:
-- MigrationId: 20251119143022_AddProductDescriptionAndCategory
-- ProductVersion: 1.0.0
-- AppliedDate: 2025-11-19 14:30:22
-- Checksum: SHA256 hash of migration content
```

## Migration File Structure

```
YourApp/
├── Migrations/
│   ├── 20251119120000_InitialCreate.cs
│   ├── 20251119143022_AddProductDescriptionAndCategory.cs
│   ├── 20251119150000_AddUserAuthentication.cs
│   └── MigrationModelSnapshot.cs  // Current schema snapshot
├── Entities/
│   ├── Product.cs
│   ├── Category.cs
│   └── User.cs
└── appsettings.json
```

## Acceptance Criteria
- [ ] Migration files generated with proper naming convention
- [ ] Up/Down methods correctly implement changes
- [ ] Migration history tracked in database
- [ ] Schema comparison accurately detects changes
- [ ] Support for rollback operations
- [ ] Idempotent migrations (safe to rerun)
- [ ] Migration checksums validated
- [ ] Complex scenarios handled (rename, data migration)
- [ ] Version control friendly output

## Dependencies
- Phase 8.1: Schema Generation from Entities
- Phase 2.6: Metadata Source Generator
- Existing: NPA.Migrations project

## Testing Requirements
- Unit tests for schema comparison
- Unit tests for migration generation
- Integration tests for Up/Down execution
- Tests for migration history tracking
- Tests for rollback scenarios
- Tests for checksum validation
- End-to-end migration workflow tests

## Documentation
- Migration generation guide
- Migration naming conventions
- Best practices for migrations
- How to handle data migrations
- Rollback strategies
- Troubleshooting guide
