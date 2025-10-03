# Phase 1.3: Simple Query Support

## 📋 Task Overview

**Objective**: Implement basic query support that allows developers to execute JPQL-like queries and get strongly-typed results using Dapper.

**Priority**: High  
**Estimated Time**: 2-3 days  
**Dependencies**: Phase 1.1 (Entity Mapping Attributes), Phase 1.2 (EntityManager)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] IQuery interface is complete
- [ ] Query class implements all query operations
- [ ] JPQL-like syntax is supported
- [ ] Parameter binding is safe and efficient
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

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
  - JPQL parsing
  - SQL generation
  - Parameter binding
  - Result mapping
  - Performance optimization

### 3. Query Parser
- **Purpose**: Parse JPQL-like queries and convert to SQL
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

### Step 4: Add JPQL Support
1. Implement basic JPQL parsing
2. Implement entity resolution
3. Implement property resolution
4. Implement join handling

### Step 5: Create Unit Tests
1. Test all query operations
2. Test JPQL parsing
3. Test parameter binding
4. Test error scenarios

### Step 6: Add Documentation
1. XML documentation comments
2. Usage examples
3. JPQL syntax guide
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
    private readonly string _jpql;
    private string? _sql;
    
    public Query(IDbConnection connection, IQueryParser parser, ISqlGenerator sqlGenerator, IParameterBinder parameterBinder, string jpql)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _parameterBinder = parameterBinder ?? throw new ArgumentNullException(nameof(parameterBinder));
        _jpql = jpql ?? throw new ArgumentNullException(nameof(jpql));
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
            var parsedQuery = _parser.Parse(_jpql);
            _sql = _sqlGenerator.Generate(parsedQuery);
        }
        return _sql;
    }
}
```

### JPQL Examples
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
- [ ] Set parameter by name
- [ ] Set parameter by index
- [ ] Handle null parameter name (should throw)
- [ ] Handle null parameter value
- [ ] Handle duplicate parameter names
- [ ] Chain parameter setting

### Query Execution Tests
- [ ] Execute select query successfully
- [ ] Execute single result query
- [ ] Execute update query
- [ ] Execute scalar query
- [ ] Handle empty result set
- [ ] Handle multiple results in single result query

### JPQL Parsing Tests
- [ ] Parse basic select query
- [ ] Parse query with where clause
- [ ] Parse query with order by
- [ ] Parse query with joins
- [ ] Handle invalid JPQL syntax
- [ ] Handle unknown entity names

### Parameter Binding Tests
- [ ] Bind string parameters
- [ ] Bind numeric parameters
- [ ] Bind date parameters
- [ ] Bind null parameters
- [ ] Handle parameter type conversion
- [ ] Handle SQL injection prevention

### Error Handling Tests
- [ ] Handle database connection errors
- [ ] Handle SQL syntax errors
- [ ] Handle parameter binding errors
- [ ] Handle timeout errors
- [ ] Handle permission errors

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic query operations
- [ ] JPQL syntax reference
- [ ] Parameter binding
- [ ] Performance considerations
- [ ] Best practices
- [ ] Error handling

### JPQL Syntax Guide
- [ ] Select statements
- [ ] Where clauses
- [ ] Order by clauses
- [ ] Join operations
- [ ] Update statements
- [ ] Delete statements

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] SQL injection prevention
- [ ] Memory usage is efficient

## 🚀 Next Steps

After completing this task:
1. Move to Phase 1.4: SQL Server Provider
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on JPQL syntax
- [ ] Performance considerations for query parsing
- [ ] Integration with Dapper optimizations
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
