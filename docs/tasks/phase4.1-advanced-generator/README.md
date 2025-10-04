# Phase 4.1: Advanced Repository Generation Patterns

## 📋 Task Overview

**Objective**: Enhance the repository source generator to support advanced patterns including complex queries, custom method implementations, and sophisticated Dapper feature integration.

**Priority**: High  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] Advanced repository patterns are supported
- [ ] Complex query generation works
- [ ] Custom method implementation is supported
- [ ] Dapper feature integration is complete
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. Advanced Query Patterns
- **Complex Joins**: Support for multiple table joins
- **Subqueries**: Support for nested queries
- **Aggregate Functions**: Support for COUNT, SUM, AVG, etc.
- **Group By**: Support for GROUP BY clauses
- **Order By**: Support for ORDER BY clauses
- **Having**: Support for HAVING clauses

### 2. Custom Method Implementation
- **QueryAttribute**: Custom SQL queries
- **StoredProcedureAttribute**: Stored procedure calls
- **BulkOperationAttribute**: Bulk operations
- **MultiMappingAttribute**: Multi-mapping queries
- **ConnectionStringAttribute**: Multiple connection strings
- **CommandTimeoutAttribute**: Command timeout configuration

### 3. Dapper Feature Integration
- **Multi-Mapping**: Complex object relationships
- **Grid Reader**: Multiple result sets
- **Dynamic Parameters**: Flexible parameter handling
- **Custom Type Handlers**: Specialized type conversion
- **Connection Management**: Multiple connections
- **Command Configuration**: Advanced command options

### 4. Performance Optimizations
- **Query Optimization**: Generate efficient SQL
- **Parameter Binding**: Optimize parameter handling
- **Connection Pooling**: Optimize connection usage
- **Caching**: Query result caching
- **Batch Operations**: Efficient batch processing

### 5. Advanced Conventions
- **Method Name Analysis**: Complex naming patterns
- **Return Type Analysis**: Sophisticated type handling
- **Parameter Analysis**: Advanced parameter binding
- **Query Generation**: Intelligent query creation

## 🏗️ Implementation Plan

### Step 1: Enhance Repository Generator
1. Update `RepositoryGenerator` class
2. Add advanced pattern detection
3. Implement complex query generation
4. Add custom method support

### Step 2: Implement Advanced Conventions
1. Create `AdvancedConventionAnalyzer` class
2. Implement complex naming patterns
3. Add return type analysis
4. Add parameter analysis

### Step 3: Add Custom Attribute Support
1. Implement `QueryAttribute` processing
2. Implement `StoredProcedureAttribute` processing
3. Implement `BulkOperationAttribute` processing
4. Implement `MultiMappingAttribute` processing

### Step 4: Implement Dapper Integration
1. Add multi-mapping support
2. Add grid reader support
3. Add dynamic parameters support
4. Add custom type handlers

### Step 5: Add Performance Optimizations
1. Implement query optimization
2. Add parameter binding optimization
3. Add connection pooling
4. Add caching support

### Step 6: Create Unit Tests
1. Test advanced patterns
2. Test custom methods
3. Test Dapper integration
4. Test performance optimizations

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Advanced patterns guide
4. Performance guide

## 📁 File Structure

```
src/NPA.Generators/RepositoryGenerator/
├── RepositoryGenerator.cs
├── AdvancedConventionAnalyzer.cs
├── CustomMethodGenerator.cs
├── DapperIntegrationGenerator.cs
├── PerformanceOptimizer.cs
└── QueryTemplate.cs

tests/NPA.Generators.Tests/RepositoryGenerator/
├── AdvancedConventionAnalyzerTests.cs
├── CustomMethodGeneratorTests.cs
├── DapperIntegrationGeneratorTests.cs
├── PerformanceOptimizerTests.cs
└── AdvancedRepositoryGeneratorTests.cs
```

## 💻 Code Examples

### Advanced Convention Analyzer
```csharp
public static class AdvancedConventionAnalyzer
{
    public static AdvancedConventionInfo? Analyze(string methodName, MethodInfo method)
    {
        // Complex naming patterns
        if (methodName.StartsWith("Find") && methodName.Contains("By") && methodName.Contains("And"))
        {
            return AnalyzeMultipleConditions(methodName, method);
        }
        
        // Aggregate functions
        if (methodName.StartsWith("Count") || methodName.StartsWith("Sum") || methodName.StartsWith("Avg"))
        {
            return AnalyzeAggregateFunction(methodName, method);
        }
        
        // Group by operations
        if (methodName.StartsWith("GroupBy"))
        {
            return AnalyzeGroupByOperation(methodName, method);
        }
        
        // Order by operations
        if (methodName.StartsWith("OrderBy"))
        {
            return AnalyzeOrderByOperation(methodName, method);
        }
        
        return null;
    }
    
    private static AdvancedConventionInfo AnalyzeMultipleConditions(string methodName, MethodInfo method)
    {
        // Extract conditions from method name
        var conditions = ExtractConditions(methodName);
        var returnType = method.ReturnType;
        
        return new AdvancedConventionInfo
        {
            Type = ConventionType.MultipleConditions,
            Conditions = conditions,
            ReturnType = returnType,
            SqlTemplate = GenerateMultipleConditionsSql(conditions)
        };
    }
    
    private static AdvancedConventionInfo AnalyzeAggregateFunction(string methodName, MethodInfo method)
    {
        var function = ExtractAggregateFunction(methodName);
        var property = ExtractPropertyName(methodName);
        
        return new AdvancedConventionInfo
        {
            Type = ConventionType.AggregateFunction,
            Function = function,
            Property = property,
            ReturnType = method.ReturnType,
            SqlTemplate = GenerateAggregateSql(function, property)
        };
    }
}
```

### Custom Method Generator
```csharp
public static class CustomMethodGenerator
{
    public static string GenerateCustomMethod(MethodInfo method, CustomAttributeInfo attribute)
    {
        return attribute.Type switch
        {
            CustomAttributeType.Query => GenerateQueryMethod(method, attribute),
            CustomAttributeType.StoredProcedure => GenerateStoredProcedureMethod(method, attribute),
            CustomAttributeType.BulkOperation => GenerateBulkOperationMethod(method, attribute),
            CustomAttributeType.MultiMapping => GenerateMultiMappingMethod(method, attribute),
            _ => throw new NotSupportedException($"Unsupported custom attribute type: {attribute.Type}")
        };
    }
    
    private static string GenerateQueryMethod(MethodInfo method, CustomAttributeInfo attribute)
    {
        var sb = new StringBuilder();
        var methodName = method.Name;
        var returnType = method.ReturnType;
        var parameters = method.Parameters;
        
        sb.AppendLine($"        public async {returnType} {methodName}({string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}"))})");
        sb.AppendLine("        {");
        sb.AppendLine($"            return await _connection.{GetDapperMethod(returnType)}(\"{attribute.Sql}\", new {{ {string.Join(", ", parameters.Select(p => $"{p.Name}"))} }});");
        sb.AppendLine("        }");
        
        return sb.ToString();
    }
    
    private static string GenerateStoredProcedureMethod(MethodInfo method, CustomAttributeInfo attribute)
    {
        var sb = new StringBuilder();
        var methodName = method.Name;
        var returnType = method.ReturnType;
        var parameters = method.Parameters;
        
        sb.AppendLine($"        public async {returnType} {methodName}({string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}"))})");
        sb.AppendLine("        {");
        sb.AppendLine($"            return await _connection.{GetDapperMethod(returnType)}(\"{attribute.ProcedureName}\", new {{ {string.Join(", ", parameters.Select(p => $"{p.Name}"))} }}, commandType: CommandType.StoredProcedure);");
        sb.AppendLine("        }");
        
        return sb.ToString();
    }
    
    private static string GenerateMultiMappingMethod(MethodInfo method, CustomAttributeInfo attribute)
    {
        var sb = new StringBuilder();
        var methodName = method.Name;
        var returnType = method.ReturnType;
        var parameters = method.Parameters;
        var mappingTypes = attribute.MappingTypes;
        
        sb.AppendLine($"        public async {returnType} {methodName}({string.Join(", ", parameters.Select(p => $"{p.Type} {p.Name}"))})");
        sb.AppendLine("        {");
        sb.AppendLine($"            return await _connection.QueryAsync<{string.Join(", ", mappingTypes)}, {returnType}>(");
        sb.AppendLine($"                \"{attribute.Sql}\",");
        sb.AppendLine($"                ({(string.Join(", ", mappingTypes.Select((t, i) => $"t{i}"))}) => new {returnType}");
        sb.AppendLine("                {");
        sb.AppendLine("                    // Mapping logic");
        sb.AppendLine("                },");
        sb.AppendLine($"                new {{ {string.Join(", ", parameters.Select(p => $"{p.Name}"))} }},");
        sb.AppendLine($"                splitOn: \"{attribute.SplitOn}\");");
        sb.AppendLine("        }");
        
        return sb.ToString();
    }
}
```

### Advanced Repository Examples
```csharp
[Repository]
public interface IUserRepository : IRepository<User, long>
{
    // Complex conditions
    Task<IEnumerable<User>> FindByUsernameAndEmailAsync(string username, string email);
    Task<IEnumerable<User>> FindByCreatedDateBetweenAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<User>> FindByRoleAndStatusAsync(string role, bool isActive);
    
    // Aggregate functions
    Task<int> CountActiveUsersAsync();
    Task<decimal> SumUserBalancesAsync();
    Task<double> AverageUserAgeAsync();
    
    // Group by operations
    Task<IEnumerable<UserGroupByRole>> GroupUsersByRoleAsync();
    Task<IEnumerable<UserGroupByStatus>> GroupUsersByStatusAsync();
    
    // Order by operations
    Task<IEnumerable<User>> FindUsersOrderByCreatedDateAsync();
    Task<IEnumerable<User>> FindUsersOrderByUsernameDescAsync();
    
    // Custom queries
    [Query("SELECT u.*, p.name as profile_name FROM users u LEFT JOIN profiles p ON u.id = p.user_id WHERE u.is_active = @active")]
    Task<IEnumerable<UserWithProfile>> FindActiveUsersWithProfilesAsync(bool active);
    
    // Stored procedures
    [StoredProcedure("sp_GetUserStatistics")]
    Task<UserStatistics> GetUserStatisticsAsync(int userId);
    
    // Bulk operations
    [BulkOperation]
    Task<int> BulkInsertUsersAsync(IEnumerable<User> users);
    
    // Multi-mapping
    [MultiMapping(typeof(User), typeof(Profile), typeof(Role))]
    Task<IEnumerable<UserWithDetails>> GetUsersWithDetailsAsync();
}
```

### Generated Advanced Repository
```csharp
// <auto-generated />
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace MyApp.Repositories
{
    public partial class IUserRepositoryImplementation : IUserRepository
    {
        private readonly IDbConnection _connection;

        public IUserRepositoryImplementation(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        // Complex conditions
        public async Task<IEnumerable<User>> FindByUsernameAndEmailAsync(string username, string email)
        {
            return await _connection.QueryAsync<User>(
                "SELECT id, username, email, created_at FROM users WHERE username = @username AND email = @email", 
                new { username, email });
        }

        // Aggregate functions
        public async Task<int> CountActiveUsersAsync()
        {
            return await _connection.QuerySingleAsync<int>(
                "SELECT COUNT(*) FROM users WHERE is_active = @active", 
                new { active = true });
        }

        // Custom queries
        public async Task<IEnumerable<UserWithProfile>> FindActiveUsersWithProfilesAsync(bool active)
        {
            return await _connection.QueryAsync<UserWithProfile>(
                "SELECT u.*, p.name as profile_name FROM users u LEFT JOIN profiles p ON u.id = p.user_id WHERE u.is_active = @active",
                new { active });
        }

        // Stored procedures
        public async Task<UserStatistics> GetUserStatisticsAsync(int userId)
        {
            return await _connection.QueryFirstOrDefaultAsync<UserStatistics>(
                "sp_GetUserStatistics", 
                new { userId }, 
                commandType: CommandType.StoredProcedure);
        }

        // Multi-mapping
        public async Task<IEnumerable<UserWithDetails>> GetUsersWithDetailsAsync()
        {
            return await _connection.QueryAsync<User, Profile, Role, UserWithDetails>(
                @"SELECT u.*, p.*, r.* FROM users u 
                  LEFT JOIN profiles p ON u.id = p.user_id 
                  LEFT JOIN user_roles ur ON u.id = ur.user_id 
                  LEFT JOIN roles r ON ur.role_id = r.id",
                (user, profile, role) => new UserWithDetails
                {
                    User = user,
                    Profile = profile,
                    Role = role
                },
                splitOn: "id,id");
        }
    }
}
```

## 🧪 Test Cases

### Advanced Pattern Tests
- [ ] Complex condition patterns
- [ ] Aggregate function patterns
- [ ] Group by patterns
- [ ] Order by patterns
- [ ] Multiple condition patterns

### Custom Method Tests
- [ ] Query attribute methods
- [ ] Stored procedure methods
- [ ] Bulk operation methods
- [ ] Multi-mapping methods
- [ ] Connection string methods

### Dapper Integration Tests
- [ ] Multi-mapping functionality
- [ ] Grid reader functionality
- [ ] Dynamic parameters
- [ ] Custom type handlers
- [ ] Connection management

### Performance Tests
- [ ] Query optimization
- [ ] Parameter binding optimization
- [ ] Connection pooling
- [ ] Caching functionality
- [ ] Batch operations

### Error Handling Tests
- [ ] Invalid method signatures
- [ ] Missing attributes
- [ ] Compilation errors
- [ ] Runtime errors
- [ ] Performance issues

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Advanced repository patterns
- [ ] Custom method implementation
- [ ] Dapper integration
- [ ] Performance optimization
- [ ] Best practices

### Advanced Patterns Guide
- [ ] Complex naming conventions
- [ ] Custom attributes
- [ ] Dapper features
- [ ] Performance considerations
- [ ] Common patterns

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] Generated code is efficient
- [ ] Memory usage is efficient

## 🚀 Next Steps

After completing this task:
1. Move to Phase 4.2: Query Method Generation from Naming Conventions
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on advanced patterns
- [ ] Performance considerations for complex queries
- [ ] Integration with existing Dapper features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
