# Phase 4.2: Query Method Generation

## 📋 Task Overview

**Objective**: Implement advanced query method generation that automatically creates repository methods based on method naming conventions and parameter analysis.

**Priority**: High  
**Estimated Time**: 4-5 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1 (Advanced Repository Generation Patterns)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] Query method analyzer is complete
- [ ] Query method generator is implemented
- [ ] Method naming conventions work
- [ ] Parameter analysis works
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. Query Method Analyzer
- **Purpose**: Analyze repository method signatures to determine query requirements
- **Features**:
  - Method name parsing
  - Parameter analysis
  - Return type analysis
  - Query type detection
  - SQL generation planning

### 2. Query Method Generator
- **Purpose**: Generate SQL queries and method implementations
- **Features**:
  - SQL query generation
  - Parameter binding
  - Return type mapping
  - Error handling
  - Performance optimization

### 3. Method Naming Conventions
- **Find Methods**: `FindBy*`, `GetBy*`, `QueryBy*`
- **Count Methods**: `CountBy*`, `Count*`
- **Exists Methods**: `ExistsBy*`, `Has*`
- **Delete Methods**: `DeleteBy*`, `RemoveBy*`
- **Update Methods**: `UpdateBy*`, `ModifyBy*`

### 4. Parameter Analysis
- **Property Mapping**: Map parameters to entity properties
- **Type Conversion**: Convert parameter types
- **Validation**: Validate parameters
- **Default Values**: Handle default values

### 5. Query Type Detection
- **Single Result**: Methods returning single entity
- **Multiple Results**: Methods returning collections
- **Count Queries**: Methods returning counts
- **Exists Queries**: Methods returning boolean
- **Update Queries**: Methods performing updates
- **Delete Queries**: Methods performing deletes

## 🏗️ Implementation Plan

### Step 1: Create Query Method Interfaces
1. Create `IQueryMethodAnalyzer` interface
2. Create `IQueryMethodGenerator` interface
3. Create `IQueryMethodValidator` interface
4. Create `IQueryMethodOptions` interface

### Step 2: Implement Query Method Analyzer
1. Create `QueryMethodAnalyzer` class
2. Implement method name parsing
3. Implement parameter analysis
4. Implement return type analysis

### Step 3: Implement Query Method Generator
1. Create `QueryMethodGenerator` class
2. Implement SQL generation
3. Implement parameter binding
4. Implement return type mapping

### Step 4: Add Method Naming Conventions
1. Implement find method conventions
2. Implement count method conventions
3. Implement exists method conventions
4. Implement delete method conventions

### Step 5: Add Parameter Analysis
1. Implement property mapping
2. Implement type conversion
3. Implement validation
4. Implement default values

### Step 6: Create Unit Tests
1. Test query method analyzer
2. Test query method generator
3. Test method naming conventions
4. Test parameter analysis

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Query method guide
4. Best practices

## 📁 File Structure

```
src/NPA.Generators/QueryMethods/
├── IQueryMethodAnalyzer.cs
├── QueryMethodAnalyzer.cs
├── IQueryMethodGenerator.cs
├── QueryMethodGenerator.cs
├── IQueryMethodValidator.cs
├── QueryMethodValidator.cs
├── QueryMethodOptions.cs
├── MethodNamingConventions.cs
├── ParameterAnalyzer.cs
└── QueryTypeDetector.cs

tests/NPA.Generators.Tests/QueryMethods/
├── QueryMethodAnalyzerTests.cs
├── QueryMethodGeneratorTests.cs
├── QueryMethodValidatorTests.cs
├── MethodNamingConventionsTests.cs
├── ParameterAnalyzerTests.cs
└── QueryTypeDetectorTests.cs
```

## 💻 Code Examples

### IQueryMethodAnalyzer Interface
```csharp
public interface IQueryMethodAnalyzer
{
    QueryMethodInfo AnalyzeMethod(MethodInfo method, EntityMetadata entityMetadata);
    bool IsQueryMethod(MethodInfo method);
    QueryType DetectQueryType(MethodInfo method);
    List<QueryParameter> AnalyzeParameters(MethodInfo method, EntityMetadata entityMetadata);
    string ExtractPropertyName(string methodName, string prefix);
    List<string> ExtractPropertyNames(string methodName, string prefix);
}

public class QueryMethodInfo
{
    public MethodInfo Method { get; set; }
    public QueryType QueryType { get; set; }
    public string PropertyName { get; set; }
    public List<string> PropertyNames { get; set; } = new();
    public List<QueryParameter> Parameters { get; set; } = new();
    public Type ReturnType { get; set; }
    public bool IsAsync { get; set; }
    public string SqlQuery { get; set; }
    public Dictionary<string, object> SqlParameters { get; set; } = new();
}

public enum QueryType
{
    FindSingle,
    FindMultiple,
    Count,
    Exists,
    Delete,
    Update,
    Custom
}

public class QueryParameter
{
    public string Name { get; set; }
    public Type Type { get; set; }
    public string PropertyName { get; set; }
    public string ColumnName { get; set; }
    public object DefaultValue { get; set; }
    public bool IsNullable { get; set; }
    public bool IsOptional { get; set; }
}
```

### QueryMethodAnalyzer Class
```csharp
public class QueryMethodAnalyzer : IQueryMethodAnalyzer
{
    private readonly IMethodNamingConventions _namingConventions;
    private readonly IParameterAnalyzer _parameterAnalyzer;
    private readonly IQueryTypeDetector _queryTypeDetector;
    
    public QueryMethodAnalyzer(
        IMethodNamingConventions namingConventions,
        IParameterAnalyzer parameterAnalyzer,
        IQueryTypeDetector queryTypeDetector)
    {
        _namingConventions = namingConventions ?? throw new ArgumentNullException(nameof(namingConventions));
        _parameterAnalyzer = parameterAnalyzer ?? throw new ArgumentNullException(nameof(parameterAnalyzer));
        _queryTypeDetector = queryTypeDetector ?? throw new ArgumentNullException(nameof(queryTypeDetector));
    }
    
    public QueryMethodInfo AnalyzeMethod(MethodInfo method, EntityMetadata entityMetadata)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (entityMetadata == null) throw new ArgumentNullException(nameof(entityMetadata));
        
        var methodInfo = new QueryMethodInfo
        {
            Method = method,
            IsAsync = method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)
        };
        
        // Detect query type
        methodInfo.QueryType = DetectQueryType(method);
        
        // Analyze method name
        AnalyzeMethodName(method, methodInfo);
        
        // Analyze parameters
        methodInfo.Parameters = AnalyzeParameters(method, entityMetadata);
        
        // Set return type
        methodInfo.ReturnType = methodInfo.IsAsync 
            ? method.ReturnType.GetGenericArguments()[0] 
            : method.ReturnType;
        
        return methodInfo;
    }
    
    public bool IsQueryMethod(MethodInfo method)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        
        var methodName = method.Name;
        
        return _namingConventions.IsFindMethod(methodName) ||
               _namingConventions.IsCountMethod(methodName) ||
               _namingConventions.IsExistsMethod(methodName) ||
               _namingConventions.IsDeleteMethod(methodName) ||
               _namingConventions.IsUpdateMethod(methodName);
    }
    
    public QueryType DetectQueryType(MethodInfo method)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        
        return _queryTypeDetector.DetectQueryType(method);
    }
    
    public List<QueryParameter> AnalyzeParameters(MethodInfo method, EntityMetadata entityMetadata)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        if (entityMetadata == null) throw new ArgumentNullException(nameof(entityMetadata));
        
        return _parameterAnalyzer.AnalyzeParameters(method, entityMetadata);
    }
    
    public string ExtractPropertyName(string methodName, string prefix)
    {
        if (string.IsNullOrEmpty(methodName)) throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));
        
        if (methodName.StartsWith(prefix))
        {
            var propertyName = methodName.Substring(prefix.Length);
            return ToPascalCase(propertyName);
        }
        
        return null;
    }
    
    public List<string> ExtractPropertyNames(string methodName, string prefix)
    {
        if (string.IsNullOrEmpty(methodName)) throw new ArgumentException("Method name cannot be null or empty", nameof(methodName));
        if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));
        
        var propertyNames = new List<string>();
        
        if (methodName.StartsWith(prefix))
        {
            var suffix = methodName.Substring(prefix.Length);
            var parts = SplitByAnd(suffix);
            
            foreach (var part in parts)
            {
                var propertyName = ToPascalCase(part);
                propertyNames.Add(propertyName);
            }
        }
        
        return propertyNames;
    }
    
    private void AnalyzeMethodName(MethodInfo method, QueryMethodInfo methodInfo)
    {
        var methodName = method.Name;
        
        if (_namingConventions.IsFindMethod(methodName))
        {
            methodInfo.PropertyNames = ExtractPropertyNames(methodName, "FindBy");
        }
        else if (_namingConventions.IsCountMethod(methodName))
        {
            methodInfo.PropertyNames = ExtractPropertyNames(methodName, "CountBy");
        }
        else if (_namingConventions.IsExistsMethod(methodName))
        {
            methodInfo.PropertyNames = ExtractPropertyNames(methodName, "ExistsBy");
        }
        else if (_namingConventions.IsDeleteMethod(methodName))
        {
            methodInfo.PropertyNames = ExtractPropertyNames(methodName, "DeleteBy");
        }
        else if (_namingConventions.IsUpdateMethod(methodName))
        {
            methodInfo.PropertyNames = ExtractPropertyNames(methodName, "UpdateBy");
        }
    }
    
    private string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        return char.ToUpper(input[0]) + input.Substring(1);
    }
    
    private List<string> SplitByAnd(string input)
    {
        var parts = new List<string>();
        var currentPart = new StringBuilder();
        
        for (int i = 0; i < input.Length; i++)
        {
            var c = input[i];
            
            if (char.IsUpper(c) && currentPart.Length > 0)
            {
                parts.Add(currentPart.ToString());
                currentPart.Clear();
            }
            
            currentPart.Append(c);
        }
        
        if (currentPart.Length > 0)
        {
            parts.Add(currentPart.ToString());
        }
        
        return parts;
    }
}
```

### MethodNamingConventions Class
```csharp
public class MethodNamingConventions : IMethodNamingConventions
{
    private readonly string[] _findPrefixes = { "FindBy", "GetBy", "QueryBy", "SearchBy" };
    private readonly string[] _countPrefixes = { "CountBy", "Count" };
    private readonly string[] _existsPrefixes = { "ExistsBy", "Has", "Contains" };
    private readonly string[] _deletePrefixes = { "DeleteBy", "RemoveBy" };
    private readonly string[] _updatePrefixes = { "UpdateBy", "ModifyBy" };
    
    public bool IsFindMethod(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return false;
        
        return _findPrefixes.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool IsCountMethod(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return false;
        
        return _countPrefixes.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool IsExistsMethod(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return false;
        
        return _existsPrefixes.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool IsDeleteMethod(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return false;
        
        return _deletePrefixes.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public bool IsUpdateMethod(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return false;
        
        return _updatePrefixes.Any(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public string GetFindPrefix(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return null;
        
        return _findPrefixes.FirstOrDefault(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public string GetCountPrefix(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return null;
        
        return _countPrefixes.FirstOrDefault(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public string GetExistsPrefix(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return null;
        
        return _existsPrefixes.FirstOrDefault(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public string GetDeletePrefix(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return null;
        
        return _deletePrefixes.FirstOrDefault(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
    
    public string GetUpdatePrefix(string methodName)
    {
        if (string.IsNullOrEmpty(methodName)) return null;
        
        return _updatePrefixes.FirstOrDefault(prefix => methodName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
```

### QueryMethodGenerator Class
```csharp
public class QueryMethodGenerator : IQueryMethodGenerator
{
    private readonly IQueryMethodAnalyzer _analyzer;
    private readonly ISqlGenerator _sqlGenerator;
    private readonly IParameterMapper _parameterMapper;
    
    public QueryMethodGenerator(
        IQueryMethodAnalyzer analyzer,
        ISqlGenerator sqlGenerator,
        IParameterMapper parameterMapper)
    {
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
        _sqlGenerator = sqlGenerator ?? throw new ArgumentNullException(nameof(sqlGenerator));
        _parameterMapper = parameterMapper ?? throw new ArgumentNullException(nameof(parameterMapper));
    }
    
    public string GenerateMethod(QueryMethodInfo methodInfo, EntityMetadata entityMetadata)
    {
        if (methodInfo == null) throw new ArgumentNullException(nameof(methodInfo));
        if (entityMetadata == null) throw new ArgumentNullException(nameof(entityMetadata));
        
        var method = methodInfo.Method;
        var methodName = method.Name;
        var parameters = method.GetParameters();
        var returnType = methodInfo.ReturnType;
        var isAsync = methodInfo.IsAsync;
        
        var sb = new StringBuilder();
        
        // Method signature
        sb.AppendLine($"public {(isAsync ? "async " : "")}{GetReturnTypeString(returnType, isAsync)} {methodName}({GetParameterString(parameters)})");
        sb.AppendLine("{");
        sb.AppendLine("    try");
        sb.AppendLine("    {");
        
        // Generate SQL query
        var sqlQuery = GenerateSqlQuery(methodInfo, entityMetadata);
        sb.AppendLine($"        var sql = \"{sqlQuery}\";");
        
        // Generate parameters
        var parameterMapping = GenerateParameterMapping(methodInfo, parameters);
        sb.AppendLine($"        var parameters = new {{ {parameterMapping} }};");
        
        // Generate method body
        GenerateMethodBody(sb, methodInfo, entityMetadata, isAsync);
        
        sb.AppendLine("    }");
        sb.AppendLine("    catch (Exception ex)");
        sb.AppendLine("    {");
        sb.AppendLine("        throw new RepositoryException($"Error executing {methodName}: {ex.Message}", ex);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }
    
    private string GenerateSqlQuery(QueryMethodInfo methodInfo, EntityMetadata entityMetadata)
    {
        var queryType = methodInfo.QueryType;
        var propertyNames = methodInfo.PropertyNames;
        var tableName = entityMetadata.TableName;
        var columns = string.Join(", ", entityMetadata.Properties.Select(p => p.ColumnName));
        
        switch (queryType)
        {
            case QueryType.FindSingle:
                return GenerateFindSingleSql(tableName, columns, propertyNames);
            case QueryType.FindMultiple:
                return GenerateFindMultipleSql(tableName, columns, propertyNames);
            case QueryType.Count:
                return GenerateCountSql(tableName, propertyNames);
            case QueryType.Exists:
                return GenerateExistsSql(tableName, propertyNames);
            case QueryType.Delete:
                return GenerateDeleteSql(tableName, propertyNames);
            case QueryType.Update:
                return GenerateUpdateSql(tableName, propertyNames);
            default:
                throw new NotSupportedException($"Query type {queryType} is not supported");
        }
    }
    
    private string GenerateFindSingleSql(string tableName, string columns, List<string> propertyNames)
    {
        var whereClause = GenerateWhereClause(propertyNames);
        return $"SELECT {columns} FROM {tableName} WHERE {whereClause}";
    }
    
    private string GenerateFindMultipleSql(string tableName, string columns, List<string> propertyNames)
    {
        var whereClause = GenerateWhereClause(propertyNames);
        return $"SELECT {columns} FROM {tableName} WHERE {whereClause}";
    }
    
    private string GenerateCountSql(string tableName, List<string> propertyNames)
    {
        var whereClause = GenerateWhereClause(propertyNames);
        return $"SELECT COUNT(*) FROM {tableName} WHERE {whereClause}";
    }
    
    private string GenerateExistsSql(string tableName, List<string> propertyNames)
    {
        var whereClause = GenerateWhereClause(propertyNames);
        return $"SELECT 1 FROM {tableName} WHERE {whereClause}";
    }
    
    private string GenerateDeleteSql(string tableName, List<string> propertyNames)
    {
        var whereClause = GenerateWhereClause(propertyNames);
        return $"DELETE FROM {tableName} WHERE {whereClause}";
    }
    
    private string GenerateUpdateSql(string tableName, List<string> propertyNames)
    {
        var whereClause = GenerateWhereClause(propertyNames);
        return $"UPDATE {tableName} SET {whereClause} WHERE {whereClause}";
    }
    
    private string GenerateWhereClause(List<string> propertyNames)
    {
        if (propertyNames == null || !propertyNames.Any())
        {
            return "1 = 1";
        }
        
        var conditions = propertyNames.Select(propertyName => $"{propertyName} = @{propertyName}");
        return string.Join(" AND ", conditions);
    }
    
    private string GenerateParameterMapping(QueryMethodInfo methodInfo, ParameterInfo[] parameters)
    {
        var mappings = new List<string>();
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var propertyName = methodInfo.PropertyNames[i];
            mappings.Add($"{propertyName} = {parameter.Name}");
        }
        
        return string.Join(", ", mappings);
    }
    
    private void GenerateMethodBody(StringBuilder sb, QueryMethodInfo methodInfo, EntityMetadata entityMetadata, bool isAsync)
    {
        var queryType = methodInfo.QueryType;
        var returnType = methodInfo.ReturnType;
        
        switch (queryType)
        {
            case QueryType.FindSingle:
                GenerateFindSingleMethodBody(sb, returnType, isAsync);
                break;
            case QueryType.FindMultiple:
                GenerateFindMultipleMethodBody(sb, returnType, isAsync);
                break;
            case QueryType.Count:
                GenerateCountMethodBody(sb, isAsync);
                break;
            case QueryType.Exists:
                GenerateExistsMethodBody(sb, isAsync);
                break;
            case QueryType.Delete:
                GenerateDeleteMethodBody(sb, isAsync);
                break;
            case QueryType.Update:
                GenerateUpdateMethodBody(sb, isAsync);
                break;
        }
    }
    
    private void GenerateFindSingleMethodBody(StringBuilder sb, Type returnType, bool isAsync)
    {
        if (isAsync)
        {
            sb.AppendLine("        return await _connection.QueryFirstOrDefaultAsync<" + returnType.Name + ">(sql, parameters);");
        }
        else
        {
            sb.AppendLine("        return _connection.QueryFirstOrDefault<" + returnType.Name + ">(sql, parameters);");
        }
    }
    
    private void GenerateFindMultipleMethodBody(StringBuilder sb, Type returnType, bool isAsync)
    {
        if (isAsync)
        {
            sb.AppendLine("        return await _connection.QueryAsync<" + returnType.Name + ">(sql, parameters);");
        }
        else
        {
            sb.AppendLine("        return _connection.Query<" + returnType.Name + ">(sql, parameters);");
        }
    }
    
    private void GenerateCountMethodBody(StringBuilder sb, bool isAsync)
    {
        if (isAsync)
        {
            sb.AppendLine("        return await _connection.QuerySingleAsync<int>(sql, parameters);");
        }
        else
        {
            sb.AppendLine("        return _connection.QuerySingle<int>(sql, parameters);");
        }
    }
    
    private void GenerateExistsMethodBody(StringBuilder sb, bool isAsync)
    {
        if (isAsync)
        {
            sb.AppendLine("        var result = await _connection.QueryFirstOrDefaultAsync<int?>(sql, parameters);");
            sb.AppendLine("        return result.HasValue;");
        }
        else
        {
            sb.AppendLine("        var result = _connection.QueryFirstOrDefault<int?>(sql, parameters);");
            sb.AppendLine("        return result.HasValue;");
        }
    }
    
    private void GenerateDeleteMethodBody(StringBuilder sb, bool isAsync)
    {
        if (isAsync)
        {
            sb.AppendLine("        return await _connection.ExecuteAsync(sql, parameters);");
        }
        else
        {
            sb.AppendLine("        return _connection.Execute(sql, parameters);");
        }
    }
    
    private void GenerateUpdateMethodBody(StringBuilder sb, bool isAsync)
    {
        if (isAsync)
        {
            sb.AppendLine("        return await _connection.ExecuteAsync(sql, parameters);");
        }
        else
        {
            sb.AppendLine("        return _connection.Execute(sql, parameters);");
        }
    }
    
    private string GetReturnTypeString(Type returnType, bool isAsync)
    {
        if (isAsync)
        {
            return $"Task<{returnType.Name}>";
        }
        
        return returnType.Name;
    }
    
    private string GetParameterString(ParameterInfo[] parameters)
    {
        var parameterStrings = parameters.Select(p => $"{p.ParameterType.Name} {p.Name}");
        return string.Join(", ", parameterStrings);
    }
}
```

### Usage Examples
```csharp
// Repository interface with query methods
public interface IUserRepository : IRepository<User, long>
{
    // Find methods
    Task<User> FindByUsernameAsync(string username);
    Task<User> FindByEmailAsync(string email);
    Task<IEnumerable<User>> FindByRoleAsync(string role);
    Task<IEnumerable<User>> FindByRoleAndStatusAsync(string role, bool isActive);
    Task<IEnumerable<User>> FindByCreatedDateRangeAsync(DateTime startDate, DateTime endDate);
    
    // Count methods
    Task<int> CountByRoleAsync(string role);
    Task<int> CountByStatusAsync(bool isActive);
    Task<int> CountByRoleAndStatusAsync(string role, bool isActive);
    
    // Exists methods
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> HasRoleAsync(long userId, string role);
    
    // Delete methods
    Task<int> DeleteByRoleAsync(string role);
    Task<int> DeleteByStatusAsync(bool isActive);
    Task<int> DeleteByRoleAndStatusAsync(string role, bool isActive);
    
    // Update methods
    Task<int> UpdateByRoleAsync(string role, bool isActive);
    Task<int> UpdateByStatusAsync(bool isActive, string role);
}

// Generated repository implementation
public partial class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;
    
    public UserRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    public async Task<User> FindByUsernameAsync(string username)
    {
        try
        {
            var sql = "SELECT id, username, email, role, is_active, created_at FROM users WHERE username = @username";
            var parameters = new { username };
            return await _connection.QueryFirstOrDefaultAsync<User>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing FindByUsernameAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<User> FindByEmailAsync(string email)
    {
        try
        {
            var sql = "SELECT id, username, email, role, is_active, created_at FROM users WHERE email = @email";
            var parameters = new { email };
            return await _connection.QueryFirstOrDefaultAsync<User>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing FindByEmailAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<IEnumerable<User>> FindByRoleAsync(string role)
    {
        try
        {
            var sql = "SELECT id, username, email, role, is_active, created_at FROM users WHERE role = @role";
            var parameters = new { role };
            return await _connection.QueryAsync<User>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing FindByRoleAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<IEnumerable<User>> FindByRoleAndStatusAsync(string role, bool isActive)
    {
        try
        {
            var sql = "SELECT id, username, email, role, is_active, created_at FROM users WHERE role = @role AND is_active = @isActive";
            var parameters = new { role, isActive };
            return await _connection.QueryAsync<User>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing FindByRoleAndStatusAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<IEnumerable<User>> FindByCreatedDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var sql = "SELECT id, username, email, role, is_active, created_at FROM users WHERE created_at >= @startDate AND created_at <= @endDate";
            var parameters = new { startDate, endDate };
            return await _connection.QueryAsync<User>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing FindByCreatedDateRangeAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> CountByRoleAsync(string role)
    {
        try
        {
            var sql = "SELECT COUNT(*) FROM users WHERE role = @role";
            var parameters = new { role };
            return await _connection.QuerySingleAsync<int>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing CountByRoleAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> CountByStatusAsync(bool isActive)
    {
        try
        {
            var sql = "SELECT COUNT(*) FROM users WHERE is_active = @isActive";
            var parameters = new { isActive };
            return await _connection.QuerySingleAsync<int>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing CountByStatusAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> CountByRoleAndStatusAsync(string role, bool isActive)
    {
        try
        {
            var sql = "SELECT COUNT(*) FROM users WHERE role = @role AND is_active = @isActive";
            var parameters = new { role, isActive };
            return await _connection.QuerySingleAsync<int>(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing CountByRoleAndStatusAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        try
        {
            var sql = "SELECT 1 FROM users WHERE username = @username";
            var parameters = new { username };
            var result = await _connection.QueryFirstOrDefaultAsync<int?>(sql, parameters);
            return result.HasValue;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing ExistsByUsernameAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<bool> ExistsByEmailAsync(string email)
    {
        try
        {
            var sql = "SELECT 1 FROM users WHERE email = @email";
            var parameters = new { email };
            var result = await _connection.QueryFirstOrDefaultAsync<int?>(sql, parameters);
            return result.HasValue;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing ExistsByEmailAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<bool> HasRoleAsync(long userId, string role)
    {
        try
        {
            var sql = "SELECT 1 FROM users WHERE id = @userId AND role = @role";
            var parameters = new { userId, role };
            var result = await _connection.QueryFirstOrDefaultAsync<int?>(sql, parameters);
            return result.HasValue;
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing HasRoleAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> DeleteByRoleAsync(string role)
    {
        try
        {
            var sql = "DELETE FROM users WHERE role = @role";
            var parameters = new { role };
            return await _connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing DeleteByRoleAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> DeleteByStatusAsync(bool isActive)
    {
        try
        {
            var sql = "DELETE FROM users WHERE is_active = @isActive";
            var parameters = new { isActive };
            return await _connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing DeleteByStatusAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> DeleteByRoleAndStatusAsync(string role, bool isActive)
    {
        try
        {
            var sql = "DELETE FROM users WHERE role = @role AND is_active = @isActive";
            var parameters = new { role, isActive };
            return await _connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing DeleteByRoleAndStatusAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> UpdateByRoleAsync(string role, bool isActive)
    {
        try
        {
            var sql = "UPDATE users SET is_active = @isActive WHERE role = @role";
            var parameters = new { role, isActive };
            return await _connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing UpdateByRoleAsync: {ex.Message}", ex);
        }
    }
    
    public async Task<int> UpdateByStatusAsync(bool isActive, string role)
    {
        try
        {
            var sql = "UPDATE users SET role = @role WHERE is_active = @isActive";
            var parameters = new { isActive, role };
            return await _connection.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            throw new RepositoryException($"Error executing UpdateByStatusAsync: {ex.Message}", ex);
        }
    }
}
```

## 🧪 Test Cases

### Query Method Analyzer Tests
- [ ] Method name parsing
- [ ] Parameter analysis
- [ ] Return type analysis
- [ ] Query type detection
- [ ] Property name extraction

### Query Method Generator Tests
- [ ] SQL query generation
- [ ] Parameter binding
- [ ] Return type mapping
- [ ] Error handling
- [ ] Method body generation

### Method Naming Conventions Tests
- [ ] Find method detection
- [ ] Count method detection
- [ ] Exists method detection
- [ ] Delete method detection
- [ ] Update method detection

### Parameter Analysis Tests
- [ ] Property mapping
- [ ] Type conversion
- [ ] Validation
- [ ] Default values

### Integration Tests
- [ ] End-to-end query method generation
- [ ] Generated code compilation
- [ ] Generated method execution
- [ ] Performance testing

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic query method generation
- [ ] Advanced query method generation
- [ ] Method naming conventions
- [ ] Best practices

### Query Method Guide
- [ ] Supported method patterns
- [ ] Parameter mapping
- [ ] Return type handling
- [ ] Common scenarios
- [ ] Troubleshooting

## 🔍 Code Review Checklist

- [ ] Code follows .NET naming conventions
- [ ] All public members have XML documentation
- [ ] Error handling is appropriate
- [ ] Unit tests cover all scenarios
- [ ] Code is readable and maintainable
- [ ] Performance is optimized
- [ ] Memory usage is efficient
- [ ] Thread safety considerations

## 🚀 Next Steps

After completing this task:
1. Move to Phase 4.3: Composite Key Generation
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on query method generation
- [ ] Performance considerations for code generation
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
