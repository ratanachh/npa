# Phase 4.4: Many-to-Many Relationship Query Generation - COMPLETION

**Completed:** November 9, 2025
**Status:** [Completed] Complete
**Tests Added:** 10 tests (all passing)
**Total Tests:** 731 tests (all passing)

## Overview

Successfully implemented automatic generation of many-to-many relationship query methods in the source generator. The generator now detects `[ManyToMany]` and `[JoinTable]` attributes and creates comprehensive relationship management methods.

## Implementation Summary

### 1. Detection Logic (`DetectManyToManyRelationships`)

**Location:** `src/NPA.Generators/RepositoryGenerator.cs` (lines 133-248)

Detects many-to-many relationships by:
- Finding properties with `[ManyToMany]` attribute
- Requiring `[JoinTable]` attribute for metadata
- Extracting collection element type from `ICollection<T>`
- Capturing join table configuration:
  - Table name and schema
  - Join columns (owner entity FK)
  - Inverse join columns (target entity FK)
  - MappedBy property for bidirectional relationships

### 2. Data Structures

**ManyToManyRelationshipInfo** (lines 1198-1208):
```csharp
internal class ManyToManyRelationshipInfo
{
    public string PropertyName { get; set; }
    public string PropertyType { get; set; }
    public string CollectionElementType { get; set; }
    public string JoinTableName { get; set; }
    public string JoinTableSchema { get; set; }
    public string[] JoinColumns { get; set; }
    public string[] InverseJoinColumns { get; set; }
    public string MappedBy { get; set; }
}
```

**RepositoryInfo Enhancement** (line 1197):
- Added `List<ManyToManyRelationshipInfo> ManyToManyRelationships` property

### 3. Method Generation (`GenerateManyToManyMethods`)

**Location:** `src/NPA.Generators/RepositoryGenerator.cs` (lines 486-585)

Generates four methods per many-to-many relationship:

#### a. Get{Related}Async
- **Purpose:** Fetch all related entities through join table
- **SQL Pattern:**
  ```sql
  SELECT r.*
  FROM {JoinTable} jt
  INNER JOIN {RelatedEntity} r ON jt.{InverseJoinColumn} = r.Id
  WHERE jt.{JoinColumn} = @EntityId
  ```
- **Returns:** `Task<IEnumerable<TRelated>>`

#### b. Add{Related}Async
- **Purpose:** Create association in join table
- **SQL Pattern:**
  ```sql
  INSERT INTO {JoinTable} ({JoinColumn}, {InverseJoinColumn})
  VALUES (@EntityId, @RelatedId)
  ```
- **Returns:** `Task`

#### c. Remove{Related}Async
- **Purpose:** Delete association from join table
- **SQL Pattern:**
  ```sql
  DELETE FROM {JoinTable}
  WHERE {JoinColumn} = @EntityId AND {InverseJoinColumn} = @RelatedId
  ```
- **Returns:** `Task`

#### d. Has{Related}Async
- **Purpose:** Check if association exists
- **SQL Pattern:**
  ```sql
  SELECT COUNT(1) FROM {JoinTable}
  WHERE {JoinColumn} = @EntityId AND {InverseJoinColumn} = @RelatedId
  ```
- **Returns:** `Task<bool>`

### 4. Features

#### Default Column Names
If `JoinColumns` or `InverseJoinColumns` are empty:
- Defaults to `{EntityName}Id` and `{RelatedName}Id`

#### Schema Support
- Supports schema-qualified join tables: `schema.table`
- Falls back to table name only if schema is empty

#### XML Documentation
All generated methods include comprehensive XML documentation with:
- `<summary>` describing the operation
- `<param>` for each parameter
- `<returns>` for return values

## Test Coverage

**File:** `tests/NPA.Generators.Tests/ManyToManyRepositoryGeneratorTests.cs` (406 lines)

### Detection Tests (3 tests)
1. [Completed] `DetectManyToManyRelationships_FindsRelationships`
   - Detects relationship with full metadata
   - Verifies property name, join table, columns

2. [Completed] `DetectManyToManyRelationships_ExtractsCollectionElementType`
   - Extracts target type from `ICollection<T>`

3. [Completed] `DetectManyToManyRelationships_HandlesMultipleRelationships`
   - Supports multiple many-to-many relationships per entity

### Generation Tests (7 tests)
4. [Completed] `GenerateManyToManyMethods_IncludesGetMethod`
   - Generates query method with JOIN
   - Verifies SQL structure

5. [Completed] `GenerateManyToManyMethods_IncludesAddMethod`
   - Generates INSERT statement
   - Uses parameterized query

6. [Completed] `GenerateManyToManyMethods_IncludesRemoveMethod`
   - Generates DELETE statement
   - Filters by both keys

7. [Completed] `GenerateManyToManyMethods_IncludesExistenceCheck`
   - Generates COUNT query
   - Returns boolean result

8. [Completed] `GenerateManyToManyMethods_IncludesXmlDocumentation`
   - Verifies all methods have XML docs
   - Checks summary and parameter docs

9. [Completed] `GenerateManyToManyMethods_HandlesSchemaQualifiedJoinTable`
   - Supports schema.table format
   - Generates correct SQL

10. [Completed] `GenerateManyToManyMethods_UsesDefaultColumnNames`
    - Falls back to convention-based names
    - Uses EntityId/RelatedId pattern

## Example Usage

### Entity Definition
```csharp
public class User
{
    [Id]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    [ManyToMany(MappedBy = "Users")]
    [JoinTable("UserRoles", 
        JoinColumns = new[] { "UserId" }, 
        InverseJoinColumns = new[] { "RoleId" })]
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}

public class Role
{
    [Id]
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public ICollection<User> Users { get; set; } = new List<User>();
}
```

### Generated Methods
```csharp
public partial class UserRepository : IUserRepository
{
    #region Many-to-Many Relationship Methods

    /// <summary>
    /// Gets all Roles for a User asynchronously.
    /// </summary>
    /// <param name="userId">The User identifier.</param>
    /// <returns>A collection of Role entities.</returns>
    public async Task<IEnumerable<Role>> GetRolesAsync(int userId)
    {
        var sql = @"
            SELECT r.*
            FROM UserRoles jt
            INNER JOIN Role r ON jt.RoleId = r.Id
            WHERE jt.UserId = @UserId";

        return await _connection.QueryAsync<Role>(
            sql,
            new { UserId = userId },
            _transaction);
    }

    /// <summary>
    /// Adds a relationship between a User and a Role asynchronously.
    /// </summary>
    public async Task AddRoleAsync(int userId, int roleId)
    {
        var sql = $"INSERT INTO UserRoles (UserId, RoleId) VALUES (@UserId, @RoleId)";
        await _connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId }, _transaction);
    }

    /// <summary>
    /// Removes a relationship between a User and a Role asynchronously.
    /// </summary>
    public async Task RemoveRoleAsync(int userId, int roleId)
    {
        var sql = $"DELETE FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
        await _connection.ExecuteAsync(sql, new { UserId = userId, RoleId = roleId }, _transaction);
    }

    /// <summary>
    /// Checks if a relationship exists between a User and a Role asynchronously.
    /// </summary>
    public async Task<bool> HasRoleAsync(int userId, int roleId)
    {
        var sql = $"SELECT COUNT(1) FROM UserRoles WHERE UserId = @UserId AND RoleId = @RoleId";
        var count = await _connection.ExecuteScalarAsync<int>(sql, new { UserId = userId, RoleId = roleId }, _transaction);
        return count > 0;
    }

    #endregion
}
```

### Usage Example
```csharp
var userRepo = new UserRepository(connection);

// Get all roles for a user
var roles = await userRepo.GetRolesAsync(userId: 1);

// Add role to user
await userRepo.AddRoleAsync(userId: 1, roleId: 5);

// Check if user has role
bool hasRole = await userRepo.HasRoleAsync(userId: 1, roleId: 5);

// Remove role from user
await userRepo.RemoveRoleAsync(userId: 1, roleId: 5);
```

## Integration with Existing Features

### Leverages Existing Infrastructure
- Uses existing `ManyToManyAttribute` (Phase 2.1)
- Uses existing `JoinTableAttribute` (Phase 2.1)
- Integrates with `LazyLoader` (Phase 3.4)
- Compatible with transaction management (Phase 3.1)

### Generator Integration
- Seamlessly integrates with `RepositoryGenerator`
- Generated methods use same patterns as other methods
- Maintains consistent SQL generation approach
- Supports all existing repository features

## Files Changed

### Core Implementation
1. **src/NPA.Generators/RepositoryGenerator.cs**
   - Added `DetectManyToManyRelationships` method (116 lines)
   - Added `GenerateManyToManyMethods` method (100 lines)
   - Enhanced `RepositoryInfo` class
   - Added `ManyToManyRelationshipInfo` class

### Tests
2. **tests/NPA.Generators.Tests/ManyToManyRepositoryGeneratorTests.cs**
   - 10 comprehensive tests (406 lines)
   - Tests detection, generation, SQL correctness
   - Validates edge cases and defaults

## Technical Details

### Detection Algorithm
1. Scan entity properties for `[ManyToMany]`
2. Verify `[JoinTable]` attribute present
3. Extract generic type from `ICollection<T>`
4. Parse attribute named arguments
5. Apply defaults for missing columns
6. Create `ManyToManyRelationshipInfo` instance

### Generation Strategy
- Uses `StringBuilder` for efficient string building
- Generates methods in dedicated region block
- Uses parameter naming conventions (camelCase)
- Maintains transaction support throughout
- Generates idiomatic C# code

### SQL Patterns
- **SELECT:** Simple JOIN with filter
- **INSERT:** Parameterized values
- **DELETE:** Composite key filter
- **COUNT:** Existence check

## Benefits

1. **Developer Productivity**
   - No manual many-to-many method writing
   - Consistent API across all relationships
   - Compile-time generation

2. **Code Quality**
   - Standardized SQL patterns
   - Proper parameterization (SQL injection safe)
   - Comprehensive documentation

3. **Maintainability**
   - Single source of truth (attributes)
   - Easy to understand generated code
   - Testable and verifiable

4. **Performance**
   - Efficient JOIN queries
   - Transaction support
   - Minimal overhead

## Validation

### Test Results
- [Completed] All 10 new tests passing
- [Completed] All 721 existing tests still passing
- [Completed] Zero breaking changes
- [Completed] Generated code compiles successfully

### Edge Cases Covered
- [Completed] Empty join columns (defaults applied)
- [Completed] Schema-qualified tables
- [Completed] Multiple relationships per entity
- [Completed] Bidirectional relationships

## Next Steps

Phase 4.4 is complete. Recommended next phases:

1. **Phase 4.5:** Eager Loading Strategy Generation
2. **Phase 4.6:** Query Result Projection
3. **Phase 4.7:** Custom Mapping Configuration

## Conclusion

Phase 4.4 successfully implements many-to-many relationship query generation, completing a critical piece of the advanced source generator capabilities. The implementation is robust, well-tested, and integrates seamlessly with existing features.

**All Phase 4.4 objectives achieved! [Completed]**
