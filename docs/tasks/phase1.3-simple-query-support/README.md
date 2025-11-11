# Phase 1.3: Simple Query Support

## 📋 Task Overview

**Objective**: Implement basic query support that allows developers to execute CPQL-like queries and get strongly-typed results using Dapper.

**Priority**: High  
**Estimated Time**: 2-3 days  
**Dependencies**: Phase 1.1 (Entity Mapping Attributes), Phase 1.2 (EntityManager)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [x] IQuery interface is complete
- [x] Query class implements all query operations
- [x] CPQL-like syntax is supported
- [x] Parameter binding is safe and efficient
- [x] Unit tests cover all functionality
- [x] Documentation is complete

## 📝 Detailed Requirements

### 1. IQuery Interface
- **Purpose**: Defines the contract for query operations
- **Methods**:
  - `IQuery<T> SetParameter(string name, object value)` - Set query parameter
  - `IQuery<T> SetParameter(int index, object value)` - Set parameter by index
  - `Task<IEnumerable<T>> GetResultListAsync()` - Execute query and return list
  - `Task<T?> GetSingleResultAsync()` - Execute query and return single result
  - `Task<T> GetSingleResultRequiredAsync()` - Execute query and return single result (throws if none)
  - `Task<int> ExecuteUpdateAsync()` - Execute update/delete query
  - `Task<object> ExecuteScalarAsync()` - Execute scalar query

### 2. Query Class
- **Purpose**: Implementation of query operations
- **Dependencies**: IDbConnection, IMetadataProvider, IQueryParser
- **Features**:
  - CPQL parsing
  - SQL generation
  - Parameter binding
  - Result mapping
  - Performance optimization

### 3. Query Parser
- **Purpose**: Parse CPQL-like queries and convert to SQL
- **Features**:
  - Entity name resolution
  - Property name resolution
  - Join handling
  - Where clause parsing
  - Order by parsing

### 4. SQL Generator
- **Purpose**: Generate database-specific SQL from parsed queries
- **Features**:
  - Database provider abstraction
  - SQL optimization
  - Parameter placeholder generation
  - Join optimization

## 🏗️ Implementation Plan

### Step 1: Create Interfaces
1. Create `IQuery<T>` interface
2. Create `IQueryParser` interface
3. Create `ISqlGenerator` interface
4. Create `IParameterBinder` interface

### Step 2: Implement Core Classes
1. Create `Query<T>` class
2. Create `QueryParser` class
3. Create `SqlGenerator` class
4. Create `ParameterBinder` class

### Step 3: Implement Query Operations
1. Implement parameter setting
2. Implement result execution
3. Implement scalar operations
4. Implement update operations

### Step 4: Add CPQL Support
1. Implement basic CPQL parsing
2. Implement entity resolution
3. Implement property resolution
4. Implement join handling

### Step 5: Create Unit Tests
1. Test all query operations
2. Test CPQL parsing
3. Test parameter binding
4. Test error scenarios

### Step 6: Add Documentation
1. XML documentation comments
2. Usage examples
3. CPQL syntax guide
4. Best practices

## 📁 File Structure

```
src/NPA.Core/Query/
├── IQuery.cs
├── Query.cs
├── IQueryParser.cs
├── QueryParser.cs
├── ISqlGenerator.cs
├── SqlGenerator.cs
├── IParameterBinder.cs
└── ParameterBinder.cs

tests/NPA.Core.Tests/Query/
├── QueryTests.cs
├── QueryParserTests.cs
├── SqlGeneratorTests.cs
└── ParameterBinderTests.cs
```

## 💻 Code Examples

### IQuery Interface
```csharp
public interface IQuery<T> : IDisposable
{
    IQuery<T> SetParameter(string name, object value);
    IQuery<T> SetParameter(int index, object value);
    Task<IEnumerable<T>> GetResultListAsync();
    Task<T?> GetSingleResultAsync();
    Task<T> GetSingleResultRequiredAsync();
    Task<int> ExecuteUpdateAsync();
    Task<object> ExecuteScalarAsync();
}
```

### Query Class
```csharp
public class Query<T> : IQuery<T>
{
    private readonly IDbConnection _connection;
    private readonly IQueryParser _parser;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IParameterBinder _parameterBinder;
    private readonly Dictionary<string, object> _parameters;
    private readonly string _cpql;
    private string? _sql;
    
    public Query(IDbConnection connection, IQueryParser parser, ISqlGenerator sqlGenerator, IParameterBinder parameterBinder, string cpql)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _parameterBinder = parameterBinder ?? throw new ArgumentNullException(nameof(parameterBinder));
        _cpql = cpql ?? throw new ArgumentNullException(nameof(cpql));
        _parameters = new Dictionary<string, object>();
    }
    
    public IQuery<T> SetParameter(string name, object value)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Parameter name cannot be null or empty", nameof(name));
        _parameters[name] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }
    
    public async Task<IEnumerable<T>> GetResultListAsync()
    {
        var sql = GetSql();
        var boundParameters = _parameterBinder.BindParameters(_parameters);
        return await _connection.QueryAsync<T>(sql, boundParameters);
    }
    
    public async Task<T?> GetSingleResultAsync()
    {
        var sql = GetSql();
        var boundParameters = _parameterBinder.BindParameters(_parameters);
        return await _connection.QueryFirstOrDefaultAsync<T>(sql, boundParameters);
    }
    
    private string GetSql()
    {
        if (_sql == null)
        {
            var parsedQuery = _parser.Parse(_cpql);
            _sql = _sqlGenerator.Generate(parsedQuery);
        }
        return _sql;
    }
}
```

### CPQL Examples
```csharp
// Basic select
var users = await entityManager
    .CreateQuery<User>("SELECT u FROM User u")
    .GetResultListAsync();

// Select with where clause
var activeUsers = await entityManager
    .CreateQuery<User>("SELECT u FROM User u WHERE u.IsActive = :active")
    .SetParameter("active", true)
    .GetResultListAsync();

// Select with parameters
var user = await entityManager
    .CreateQuery<User>("SELECT u FROM User u WHERE u.Username = :username")
    .SetParameter("username", "john")
    .GetSingleResultAsync();

// Update query
var updatedCount = await entityManager
    .CreateQuery<User>("UPDATE User u SET u.IsActive = :active WHERE u.CreatedAt < :date")
    .SetParameter("active", false)
    .SetParameter("date", DateTime.UtcNow.AddYears(-1))
    .ExecuteUpdateAsync();
```

## 🧪 Test Cases

### Parameter Setting Tests
- [x] Set parameter by name
- [x] Set parameter by index
- [x] Handle null parameter name (should throw)
- [x] Handle null parameter value
- [x] Handle duplicate parameter names
- [x] Chain parameter setting

### Query Execution Tests
- [x] Execute select query successfully
- [x] Execute single result query
- [x] Execute update query
- [x] Execute scalar query
- [x] Handle empty result set
- [x] Handle multiple results in single result query

### CPQL Parsing Tests
- [x] Parse basic select query
- [x] Parse query with where clause
- [x] Parse query with order by
- [x] Parse query with joins
- [x] Handle invalid CPQL syntax
- [x] Handle unknown entity names

### Parameter Binding Tests
- [x] Bind string parameters
- [x] Bind numeric parameters
- [x] Bind date parameters
- [x] Bind null parameters
- [x] Handle parameter type conversion
- [x] Handle SQL injection prevention

### Error Handling Tests
- [x] Handle database connection errors
- [x] Handle SQL syntax errors
- [x] Handle parameter binding errors
- [x] Handle timeout errors
- [x] Handle permission errors

## 📚 Documentation Requirements

### XML Documentation
- [x] All public members documented
- [x] Parameter descriptions
- [x] Return value descriptions
- [x] Exception documentation
- [x] Usage examples

### Usage Guide
- [x] Basic query operations
- [x] CPQL syntax reference
- [x] Parameter binding
- [x] Performance considerations
- [x] Best practices
- [x] Error handling

### CPQL Syntax Guide
- [x] Select statements
- [x] Where clauses
- [x] Order by clauses
- [x] Join operations
- [x] Update statements
- [x] Delete statements

## 🔍 Code Review Checklist

- [x] Code follows .NET naming conventions
- [x] All public members have XML documentation
- [x] Error handling is appropriate
- [x] Unit tests cover all scenarios
- [x] Code is readable and maintainable
- [x] Performance is optimized
- [x] SQL injection prevention
- [x] Memory usage is efficient

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.4: SQL Server Provider
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [x] Clarification needed on CPQL syntax - **RESOLVED**: Implemented basic CPQL parsing with regex patterns
- [x] Performance considerations for query parsing - **RESOLVED**: Used compiled regex patterns for performance
- [x] Integration with Dapper optimizations - **RESOLVED**: Integrated with Dapper's QueryAsync and ExecuteAsync methods
- [x] Error message localization - **RESOLVED**: Used standard .NET exception messages

## [Completed] Implementation Notes

### Completed Features
- All query interfaces and classes implemented with full XML documentation
- CPQL parsing with regex patterns for SELECT, UPDATE, DELETE queries
- SQL generation with entity metadata integration
- Parameter binding with SQL injection prevention
- Comprehensive unit test coverage
- Integration with EntityManager via CreateQuery method

### Test Coverage
- **QueryTests.cs**: Tests for all query operations, parameter setting, error handling
- **Parameter binding**: Safe parameter handling with sanitization
- **CPQL parsing**: Basic CPQL syntax support with error handling
- **SQL generation**: Database-agnostic SQL generation

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: [Completed] COMPLETED*
