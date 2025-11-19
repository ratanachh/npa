# Phase 8.4: Schema Comparison and Diff

## Overview
Compare current database schema with entity definitions to detect schema drift, generate diff reports, and create corrective migrations.

## Objectives
- Read existing database schema
- Compare with entity-based schema
- Generate detailed diff reports
- Create migrations to fix schema drift
- Validate database consistency

## Tasks

### 1. Schema Reading
- [ ] Implement `IDatabaseSchemaReader` interface
- [ ] Read tables, columns, constraints from database
- [ ] Read indexes and their configuration
- [ ] Read foreign keys and relationships
- [ ] Support all database providers

### 2. Schema Comparison Engine
- [ ] Create `SchemaComparer` to compare schemas
- [ ] Detect table differences (added, removed, modified)
- [ ] Detect column differences (type, nullable, default)
- [ ] Detect constraint differences
- [ ] Detect index differences
- [ ] Detect foreign key differences

### 3. Diff Report Generation
- [ ] Generate human-readable diff reports
- [ ] Support JSON and text output formats
- [ ] Highlight breaking vs non-breaking changes
- [ ] Calculate migration complexity score
- [ ] Generate migration preview

### 4. Schema Drift Detection
- [ ] Automatic drift detection on startup
- [ ] Configurable drift handling (error, warning, ignore)
- [ ] Drift notification system
- [ ] Historical drift tracking

### 5. Corrective Migration Generation
- [ ] Generate migrations from schema differences
- [ ] Handle complex scenarios (renames, data type changes)
- [ ] Suggest data migration strategies
- [ ] Validate generated migrations

## Example Usage

### Schema Comparison

```csharp
using NPA.Migrations.Schema;

var schemaReader = new SqlServerSchemaReader(connection);
var currentSchema = await schemaReader.ReadSchemaAsync();

var entitySchema = SchemaGenerator.GenerateFromEntities(entityTypes);

var comparer = new SchemaComparer();
var differences = comparer.Compare(currentSchema, entitySchema);

// Print diff report
var report = new SchemaReport(differences);
Console.WriteLine(report.ToString());
```

### CLI Usage

```bash
# Compare schema with entities
dotnet npa schema diff

# Output:
# Schema Comparison Report
# ========================
# Database: ProductionDB
# Compared: Database Schema vs Entity Definitions
# 
# Summary:
#   • 2 tables to be added
#   • 1 table to be modified
#   • 3 columns to be added
#   • 1 column type mismatch
#   • 2 indexes missing
# 
# Details:
# 
# [+] New Tables:
#   • orders
#   • order_items
# 
# [~] Modified Tables:
#   products:
#     [+] Add column: description (NVARCHAR(500), nullable)
#     [!] Type mismatch: price (INT → DECIMAL(18,2))
#     [+] Add index: IX_products_category_id
# 
# Risk Assessment: MEDIUM
#   - 1 data type change requires data migration
#   - 2 new tables (no data loss risk)
# 
# Recommended Action:
#   Run: dotnet npa migration add FixSchemaDrift

# Generate migration from schema drift
dotnet npa schema sync

# Output:
# ✓ Schema drift detected
# ✓ Generated migration: 20251119160000_SyncSchemaWithEntities.cs
# 
# To apply: dotnet npa migrate up

# Validate current schema
dotnet npa schema validate

# Output:
# Schema Validation
# =================
# 
# ✓ All tables exist
# ✓ All columns exist
# ✗ Type mismatch in products.price
# ✗ Missing index: IX_products_category_id
# 
# Status: INVALID
# Issues: 2
```

## Schema Reader Implementation

```csharp
public interface IDatabaseSchemaReader
{
    Task<DatabaseSchema> ReadSchemaAsync(string? schemaName = null);
    Task<TableSchema> ReadTableAsync(string tableName, string? schemaName = null);
}

public class SqlServerSchemaReader : IDatabaseSchemaReader
{
    public async Task<DatabaseSchema> ReadSchemaAsync(string? schemaName = null)
    {
        var schema = new DatabaseSchema();
        
        // Read tables
        var tables = await ReadTablesAsync(schemaName);
        foreach (var table in tables)
        {
            // Read columns
            table.Columns = await ReadColumnsAsync(table.Name, schemaName);
            
            // Read indexes
            table.Indexes = await ReadIndexesAsync(table.Name, schemaName);
            
            // Read constraints
            table.Constraints = await ReadConstraintsAsync(table.Name, schemaName);
        }
        
        // Read foreign keys
        schema.ForeignKeys = await ReadForeignKeysAsync(schemaName);
        
        return schema;
    }
    
    private async Task<List<TableInfo>> ReadTablesAsync(string? schemaName)
    {
        const string sql = @"
            SELECT 
                TABLE_SCHEMA as [Schema],
                TABLE_NAME as [Name],
                TABLE_TYPE as [Type]
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
                AND (@Schema IS NULL OR TABLE_SCHEMA = @Schema)
            ORDER BY TABLE_SCHEMA, TABLE_NAME";
        
        return (await _connection.QueryAsync<TableInfo>(sql, new { Schema = schemaName })).ToList();
    }
    
    private async Task<List<ColumnInfo>> ReadColumnsAsync(string tableName, string? schemaName)
    {
        const string sql = @"
            SELECT 
                c.COLUMN_NAME as [Name],
                c.DATA_TYPE as [DataType],
                c.CHARACTER_MAXIMUM_LENGTH as [MaxLength],
                c.NUMERIC_PRECISION as [Precision],
                c.NUMERIC_SCALE as [Scale],
                c.IS_NULLABLE as [IsNullable],
                c.COLUMN_DEFAULT as [DefaultValue],
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA + '.' + c.TABLE_NAME), c.COLUMN_NAME, 'IsIdentity') as [IsIdentity]
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE c.TABLE_NAME = @TableName
                AND (@Schema IS NULL OR c.TABLE_SCHEMA = @Schema)
            ORDER BY c.ORDINAL_POSITION";
        
        return (await _connection.QueryAsync<ColumnInfo>(sql, new 
        { 
            TableName = tableName, 
            Schema = schemaName 
        })).ToList();
    }
}
```

## Schema Comparer Implementation

```csharp
public class SchemaComparer
{
    public SchemaDifferences Compare(DatabaseSchema current, DatabaseSchema target)
    {
        var differences = new SchemaDifferences();
        
        // Compare tables
        differences.TableDifferences = CompareTables(current.Tables, target.Tables);
        
        // Compare columns
        differences.ColumnDifferences = CompareColumns(current, target);
        
        // Compare indexes
        differences.IndexDifferences = CompareIndexes(current, target);
        
        // Compare foreign keys
        differences.ForeignKeyDifferences = CompareForeignKeys(current.ForeignKeys, target.ForeignKeys);
        
        return differences;
    }
    
    private List<TableDifference> CompareTables(List<TableSchema> current, List<TableSchema> target)
    {
        var differences = new List<TableDifference>();
        
        // Find added tables
        var addedTables = target.Where(t => !current.Any(c => c.Name == t.Name));
        differences.AddRange(addedTables.Select(t => new TableDifference
        {
            TableName = t.Name,
            Type = DifferenceType.Added,
            TargetTable = t
        }));
        
        // Find removed tables
        var removedTables = current.Where(c => !target.Any(t => t.Name == c.Name));
        differences.AddRange(removedTables.Select(c => new TableDifference
        {
            TableName = c.Name,
            Type = DifferenceType.Removed,
            CurrentTable = c
        }));
        
        // Find modified tables (exist in both but may have column changes)
        var commonTables = current.Where(c => target.Any(t => t.Name == c.Name));
        foreach (var currentTable in commonTables)
        {
            var targetTable = target.First(t => t.Name == currentTable.Name);
            
            if (!AreTablesEqual(currentTable, targetTable))
            {
                differences.Add(new TableDifference
                {
                    TableName = currentTable.Name,
                    Type = DifferenceType.Modified,
                    CurrentTable = currentTable,
                    TargetTable = targetTable
                });
            }
        }
        
        return differences;
    }
    
    private List<ColumnDifference> CompareColumns(DatabaseSchema current, DatabaseSchema target)
    {
        var differences = new List<ColumnDifference>();
        
        foreach (var currentTable in current.Tables)
        {
            var targetTable = target.Tables.FirstOrDefault(t => t.Name == currentTable.Name);
            if (targetTable == null)
                continue;
            
            // Added columns
            var addedColumns = targetTable.Columns.Where(tc => 
                !currentTable.Columns.Any(cc => cc.Name == tc.Name));
            
            differences.AddRange(addedColumns.Select(c => new ColumnDifference
            {
                TableName = currentTable.Name,
                ColumnName = c.Name,
                Type = DifferenceType.Added,
                TargetColumn = c
            }));
            
            // Removed columns
            var removedColumns = currentTable.Columns.Where(cc => 
                !targetTable.Columns.Any(tc => tc.Name == cc.Name));
            
            differences.AddRange(removedColumns.Select(c => new ColumnDifference
            {
                TableName = currentTable.Name,
                ColumnName = c.Name,
                Type = DifferenceType.Removed,
                CurrentColumn = c
            }));
            
            // Modified columns
            var commonColumns = currentTable.Columns.Where(cc => 
                targetTable.Columns.Any(tc => tc.Name == cc.Name));
            
            foreach (var currentColumn in commonColumns)
            {
                var targetColumn = targetTable.Columns.First(tc => tc.Name == currentColumn.Name);
                
                if (!AreColumnsEqual(currentColumn, targetColumn))
                {
                    differences.Add(new ColumnDifference
                    {
                        TableName = currentTable.Name,
                        ColumnName = currentColumn.Name,
                        Type = DifferenceType.Modified,
                        CurrentColumn = currentColumn,
                        TargetColumn = targetColumn,
                        Changes = GetColumnChanges(currentColumn, targetColumn)
                    });
                }
            }
        }
        
        return differences;
    }
}
```

## Diff Report Models

```csharp
public class SchemaDifferences
{
    public List<TableDifference> TableDifferences { get; set; } = new();
    public List<ColumnDifference> ColumnDifferences { get; set; } = new();
    public List<IndexDifference> IndexDifferences { get; set; } = new();
    public List<ForeignKeyDifference> ForeignKeyDifferences { get; set; } = new();
    
    public bool HasDifferences => 
        TableDifferences.Any() || 
        ColumnDifferences.Any() || 
        IndexDifferences.Any() || 
        ForeignKeyDifferences.Any();
    
    public RiskLevel CalculateRiskLevel()
    {
        // Breaking changes: removing tables/columns, changing types
        var hasBreakingChanges = 
            TableDifferences.Any(d => d.Type == DifferenceType.Removed) ||
            ColumnDifferences.Any(d => d.Type == DifferenceType.Removed) ||
            ColumnDifferences.Any(d => d.Changes.Contains("DataType"));
        
        if (hasBreakingChanges)
            return RiskLevel.High;
        
        // Medium risk: adding non-nullable columns, changing constraints
        var hasMediumRisk =
            ColumnDifferences.Any(d => d.Type == DifferenceType.Added && !d.TargetColumn.IsNullable);
        
        if (hasMediumRisk)
            return RiskLevel.Medium;
        
        return RiskLevel.Low;
    }
}

public class TableDifference
{
    public string TableName { get; set; }
    public DifferenceType Type { get; set; }
    public TableSchema? CurrentTable { get; set; }
    public TableSchema? TargetTable { get; set; }
}

public class ColumnDifference
{
    public string TableName { get; set; }
    public string ColumnName { get; set; }
    public DifferenceType Type { get; set; }
    public ColumnInfo? CurrentColumn { get; set; }
    public ColumnInfo? TargetColumn { get; set; }
    public List<string> Changes { get; set; } = new();
}

public enum DifferenceType
{
    Added,
    Removed,
    Modified
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}
```

## Acceptance Criteria
- [ ] Accurately read schema from all supported databases
- [ ] Correctly compare schemas and detect all differences
- [ ] Generate clear and actionable diff reports
- [ ] Classify changes by risk level
- [ ] Generate valid migrations from differences
- [ ] Handle edge cases (renames, type changes)
- [ ] Performance acceptable for large schemas
- [ ] Support filtering and focusing on specific tables

## Dependencies
- Phase 8.1: Schema Generation from Entities
- Phase 8.2: Migration Generation
- Existing database provider implementations

## Testing Requirements
- Unit tests for schema reader
- Unit tests for schema comparer
- Integration tests with real databases
- Tests for all difference types
- Tests for complex scenarios
- Performance tests with large schemas
- Validation tests for generated migrations

## Documentation
- Schema comparison guide
- Understanding diff reports
- Risk assessment explanation
- Handling schema drift
- Best practices for schema changes
