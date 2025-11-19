# Phase 8.5: Data Migration Support

## Overview
Enable data transformations and seed data management during schema migrations to preserve data integrity and populate initial data.

## Objectives
- Transform data during schema changes
- Support seed data insertion
- Handle complex data migrations
- Preserve data integrity
- Rollback data changes safely

## Tasks

### 1. Data Migration Operations
- [ ] Implement `DataMigration` base class
- [ ] Support raw SQL execution in migrations
- [ ] Support parameterized queries for safety
- [ ] Batch data processing for large datasets
- [ ] Transaction support for data operations

### 2. Data Transformation
- [ ] Column rename with data preservation
- [ ] Data type conversion with validation
- [ ] Split/merge column data
- [ ] Computed column migration
- [ ] Conditional data transformations

### 3. Seed Data Management
- [ ] Seed data definition API
- [ ] Idempotent seed data insertion
- [ ] Environment-specific seed data
- [ ] Seed data versioning
- [ ] Seed data rollback

### 4. Data Validation
- [ ] Pre-migration data validation
- [ ] Post-migration data verification
- [ ] Data integrity checks
- [ ] Constraint validation
- [ ] Referential integrity validation

### 5. Complex Scenarios
- [ ] Handle foreign key dependencies
- [ ] Temporary column strategies
- [ ] Multi-step data migrations
- [ ] Parallel data processing
- [ ] Error recovery strategies

## Example Usage

### Basic Data Migration

```csharp
using NPA.Migrations;

public class AddUserRoles_20251119160000 : Migration
{
    public override void Up(MigrationBuilder builder)
    {
        // Add new column
        builder.AddColumn("users", "role_id", "INT");
        
        // Migrate data: assign default role to existing users
        builder.ExecuteSql(@"
            UPDATE users 
            SET role_id = (SELECT id FROM roles WHERE name = 'User')
            WHERE role_id IS NULL
        ");
        
        // Make column non-nullable
        builder.AlterColumn("users", "role_id", "INT", nullable: false);
        
        // Add foreign key
        builder.AddForeignKey("users", "role_id", "roles", "id");
    }
    
    public override void Down(MigrationBuilder builder)
    {
        builder.DropForeignKey("users", "FK_users_roles");
        builder.DropColumn("users", "role_id");
    }
}
```

### Data Transformation Migration

```csharp
public class SplitFullName_20251119160100 : Migration
{
    public override void Up(MigrationBuilder builder)
    {
        // Add new columns
        builder.AddColumn("users", "first_name", "NVARCHAR(100)");
        builder.AddColumn("users", "last_name", "NVARCHAR(100)");
        
        // Transform data: split full_name into first_name and last_name
        builder.ExecuteSql(@"
            UPDATE users 
            SET 
                first_name = SUBSTRING(full_name, 1, CHARINDEX(' ', full_name + ' ') - 1),
                last_name = LTRIM(SUBSTRING(full_name, CHARINDEX(' ', full_name + ' '), LEN(full_name)))
            WHERE full_name IS NOT NULL
        ");
        
        // Make columns non-nullable
        builder.AlterColumn("users", "first_name", "NVARCHAR(100)", nullable: false);
        builder.AlterColumn("users", "last_name", "NVARCHAR(100)", nullable: false);
        
        // Drop old column
        builder.DropColumn("users", "full_name");
    }
    
    public override void Down(MigrationBuilder builder)
    {
        // Add old column back
        builder.AddColumn("users", "full_name", "NVARCHAR(200)");
        
        // Restore data
        builder.ExecuteSql(@"
            UPDATE users 
            SET full_name = first_name + ' ' + last_name
        ");
        
        // Drop new columns
        builder.DropColumn("users", "first_name");
        builder.DropColumn("users", "last_name");
    }
}
```

### Seed Data Migration

```csharp
public class SeedInitialData_20251119160200 : DataMigration
{
    public override void Up(MigrationBuilder builder)
    {
        // Seed roles
        builder.InsertData("roles", new[]
        {
            new Dictionary<string, object>
            {
                ["id"] = 1,
                ["name"] = "Admin",
                ["description"] = "Administrator role"
            },
            new Dictionary<string, object>
            {
                ["id"] = 2,
                ["name"] = "User",
                ["description"] = "Standard user role"
            },
            new Dictionary<string, object>
            {
                ["id"] = 3,
                ["name"] = "Guest",
                ["description"] = "Guest user role"
            }
        });
        
        // Seed default admin user
        builder.InsertData("users", new[]
        {
            new Dictionary<string, object>
            {
                ["id"] = 1,
                ["username"] = "admin",
                ["email"] = "admin@example.com",
                ["role_id"] = 1,
                ["created_at"] = DateTime.UtcNow
            }
        });
    }
    
    public override void Down(MigrationBuilder builder)
    {
        builder.DeleteData("users", new { id = 1 });
        builder.DeleteData("roles", new { id = new[] { 1, 2, 3 } });
    }
}
```

### Batch Data Processing

```csharp
public class MigrateProductPrices_20251119160300 : Migration
{
    public override void Up(MigrationBuilder builder)
    {
        // Add new decimal column
        builder.AddColumn("products", "price_decimal", "DECIMAL(18,2)");
        
        // Migrate data in batches to avoid memory issues
        builder.ExecuteBatch(@"
            UPDATE products 
            SET price_decimal = CAST(price_integer AS DECIMAL(18,2)) / 100
            WHERE price_decimal IS NULL
        ", batchSize: 1000);
        
        // Make non-nullable
        builder.AlterColumn("products", "price_decimal", "DECIMAL(18,2)", nullable: false);
        
        // Drop old column
        builder.DropColumn("products", "price_integer");
        
        // Rename new column
        builder.RenameColumn("products", "price_decimal", "price");
    }
}
```

## Data Migration Builder API

```csharp
public class MigrationBuilder
{
    // Raw SQL execution
    public void ExecuteSql(string sql, object? parameters = null)
    {
        var command = _connection.CreateCommand();
        command.CommandText = sql;
        
        if (parameters != null)
        {
            AddParameters(command, parameters);
        }
        
        command.ExecuteNonQuery();
    }
    
    // Batch execution with progress
    public void ExecuteBatch(string sql, int batchSize = 1000, Action<int>? progressCallback = null)
    {
        int processed = 0;
        int affected;
        
        do
        {
            var batchSql = $@"
                {sql}
                AND id IN (
                    SELECT TOP {batchSize} id 
                    FROM products 
                    WHERE price_decimal IS NULL
                )";
            
            affected = ExecuteSqlWithResult(batchSql);
            processed += affected;
            
            progressCallback?.Invoke(processed);
            
        } while (affected > 0);
    }
    
    // Insert seed data
    public void InsertData(string tableName, IEnumerable<Dictionary<string, object>> rows)
    {
        foreach (var row in rows)
        {
            var columns = string.Join(", ", row.Keys);
            var parameters = string.Join(", ", row.Keys.Select(k => $"@{k}"));
            
            var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({parameters})";
            
            ExecuteSql(sql, row);
        }
    }
    
    // Update data
    public void UpdateData(string tableName, Dictionary<string, object> values, object where)
    {
        var setClauses = string.Join(", ", values.Keys.Select(k => $"{k} = @{k}"));
        var whereClause = BuildWhereClause(where);
        
        var sql = $"UPDATE {tableName} SET {setClauses} WHERE {whereClause}";
        
        var allParams = new Dictionary<string, object>(values);
        AddWhereParameters(allParams, where);
        
        ExecuteSql(sql, allParams);
    }
    
    // Delete data
    public void DeleteData(string tableName, object where)
    {
        var whereClause = BuildWhereClause(where);
        var sql = $"DELETE FROM {tableName} WHERE {whereClause}";
        
        ExecuteSql(sql, where);
    }
    
    // Conditional execution
    public void ExecuteIf(Func<bool> condition, Action<MigrationBuilder> migration)
    {
        if (condition())
        {
            migration(this);
        }
    }
    
    // Data validation
    public bool ValidateData(string sql, string errorMessage)
    {
        var result = ExecuteScalar<int>(sql);
        
        if (result > 0)
        {
            throw new MigrationException($"Data validation failed: {errorMessage}");
        }
        
        return true;
    }
}
```

## Seed Data Configuration

```csharp
public class SeedDataConfiguration
{
    public string Environment { get; set; }
    public List<SeedDataDefinition> Seeds { get; set; } = new();
}

public class SeedDataDefinition
{
    public string Table { get; set; }
    public List<Dictionary<string, object>> Data { get; set; } = new();
    public bool IdempotentInsert { get; set; } = true;
    public string? CheckExistsSql { get; set; }
}

// Usage in configuration
public class DatabaseSeeder
{
    public void Configure(SeedDataConfiguration config)
    {
        config.Seeds.Add(new SeedDataDefinition
        {
            Table = "roles",
            IdempotentInsert = true,
            CheckExistsSql = "SELECT COUNT(*) FROM roles WHERE id = @id",
            Data = new List<Dictionary<string, object>>
            {
                new() { ["id"] = 1, ["name"] = "Admin" },
                new() { ["id"] = 2, ["name"] = "User" }
            }
        });
    }
}
```

## CLI Usage

```bash
# Create data migration
dotnet npa migration add MigrateUserData --data

# Create seed data migration
dotnet npa migration add SeedInitialData --seed

# Apply with data validation
dotnet npa migrate up --validate-data

# Seed data only
dotnet npa database seed

# Seed specific environment
dotnet npa database seed --environment Production

# Validate data integrity
dotnet npa database validate
```

## Data Migration Testing

```csharp
[Test]
public async Task DataMigration_Should_PreserveDataIntegrity()
{
    // Arrange
    await SeedTestData();
    var beforeCount = await CountRecords("users");
    
    // Act
    await _migrator.UpAsync("AddUserRoles_20251119160000");
    
    // Assert
    var afterCount = await CountRecords("users");
    Assert.That(afterCount, Is.EqualTo(beforeCount));
    
    var usersWithoutRole = await CountRecords("users", "role_id IS NULL");
    Assert.That(usersWithoutRole, Is.Zero);
}

[Test]
public async Task SeedData_Should_BeIdempotent()
{
    // Act - run seed twice
    await _migrator.UpAsync("SeedInitialData_20251119160200");
    await _migrator.UpAsync("SeedInitialData_20251119160200");
    
    // Assert - data should not be duplicated
    var roleCount = await CountRecords("roles");
    Assert.That(roleCount, Is.EqualTo(3));
}
```

## Complex Data Migration Example

```csharp
public class NormalizeAddresses_20251119160400 : Migration
{
    public override void Up(MigrationBuilder builder)
    {
        // Step 1: Create addresses table
        builder.CreateTable("addresses", table => new
        {
            id = table.Int32().PrimaryKey().Identity(),
            street = table.String(200).NotNull(),
            city = table.String(100).NotNull(),
            state = table.String(50).NotNull(),
            zip_code = table.String(20).NotNull(),
            country = table.String(100).NotNull().Default("USA")
        });
        
        // Step 2: Add address_id to users table
        builder.AddColumn("users", "address_id", "INT");
        
        // Step 3: Migrate existing address data
        builder.ExecuteSql(@"
            INSERT INTO addresses (street, city, state, zip_code, country)
            SELECT DISTINCT street, city, state, zip_code, COALESCE(country, 'USA')
            FROM users
            WHERE street IS NOT NULL
        ");
        
        // Step 4: Update users with address_id
        builder.ExecuteSql(@"
            UPDATE u
            SET u.address_id = a.id
            FROM users u
            INNER JOIN addresses a ON 
                u.street = a.street AND
                u.city = a.city AND
                u.state = a.state AND
                u.zip_code = a.zip_code
        ");
        
        // Step 5: Validate no users without address
        builder.ValidateData(
            "SELECT COUNT(*) FROM users WHERE street IS NOT NULL AND address_id IS NULL",
            "Some users were not migrated to addresses table"
        );
        
        // Step 6: Add foreign key
        builder.AddForeignKey("users", "address_id", "addresses", "id");
        
        // Step 7: Drop old columns
        builder.DropColumn("users", "street");
        builder.DropColumn("users", "city");
        builder.DropColumn("users", "state");
        builder.DropColumn("users", "zip_code");
        builder.DropColumn("users", "country");
    }
    
    public override void Down(MigrationBuilder builder)
    {
        // Restore old structure
        builder.AddColumn("users", "street", "NVARCHAR(200)");
        builder.AddColumn("users", "city", "NVARCHAR(100)");
        builder.AddColumn("users", "state", "NVARCHAR(50)");
        builder.AddColumn("users", "zip_code", "NVARCHAR(20)");
        builder.AddColumn("users", "country", "NVARCHAR(100)");
        
        // Restore data
        builder.ExecuteSql(@"
            UPDATE u
            SET 
                u.street = a.street,
                u.city = a.city,
                u.state = a.state,
                u.zip_code = a.zip_code,
                u.country = a.country
            FROM users u
            INNER JOIN addresses a ON u.address_id = a.id
        ");
        
        // Drop foreign key and column
        builder.DropForeignKey("users", "FK_users_addresses");
        builder.DropColumn("users", "address_id");
        
        // Drop addresses table
        builder.DropTable("addresses");
    }
}
```

## Acceptance Criteria
- [ ] Support raw SQL execution in migrations
- [ ] Batch processing for large datasets
- [ ] Idempotent seed data insertion
- [ ] Environment-specific seed data
- [ ] Data validation before and after migration
- [ ] Safe rollback of data changes
- [ ] Handle foreign key dependencies
- [ ] Performance acceptable for large datasets
- [ ] Transaction support for data operations
- [ ] Clear error messages for data issues

## Dependencies
- Phase 8.1: Schema Generation from Entities
- Phase 8.2: Migration Generation
- Phase 8.3: Migration Execution Engine

## Testing Requirements
- Unit tests for data migration operations
- Integration tests with real data
- Test idempotent seed data
- Test rollback of data changes
- Performance tests with large datasets
- Test foreign key dependency handling
- Test data validation failures
- Test batch processing

## Documentation
- Data migration guide
- Seed data configuration
- Best practices for data transformations
- Handling complex scenarios
- Performance optimization tips
- Rollback strategies
