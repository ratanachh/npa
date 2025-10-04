# Phase 1.6: Repository Source Generator (Basic)

## 📋 Task Overview

**Objective**: Implement a basic source generator that automatically generates repository implementations from interfaces marked with the `[Repository]` attribute.

**Priority**: High  
**Estimated Time**: 3-4 days  
**Dependencies**: Phase 1.1-1.5 (All previous phases)  
**Target Framework**: .NET 6.0  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] RepositoryGenerator class is complete
- [ ] Basic repository implementations are generated
- [ ] Convention-based method generation works
- [ ] Generated code is type-safe and efficient
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. RepositoryGenerator Class
- **Purpose**: Main source generator that processes repository interfaces
- **Features**:
  - Interface detection
  - Method analysis
  - Code generation
  - Incremental processing
  - Error handling

### 2. RepositorySyntaxReceiver
- **Purpose**: Receives syntax nodes during compilation
- **Features**:
  - Interface detection
  - Method collection
  - Attribute analysis
  - Syntax validation

### 3. RepositoryCodeGenerator
- **Purpose**: Generates repository implementation code
- **Features**:
  - Method implementation generation
  - Convention-based query generation
  - Parameter binding
  - Error handling

### 4. Convention-Based Generation
- **Patterns**:
  - `FindBy{Property}Async` → `WHERE {property} = @{property}`
  - `Find{Property}ContainingAsync` → `WHERE {property} LIKE '%@{property}%'`
  - `Find{Property}StartingWithAsync` → `WHERE {property} LIKE '@{property}%'`
  - `Find{Property}EndingWithAsync` → `WHERE {property} LIKE '%@{property}'`
  - `FindBy{Property}GreaterThanAsync` → `WHERE {property} > @{property}`
  - `FindBy{Property}LessThanAsync` → `WHERE {property} < @{property}`

### 5. Generated Code Features
- **Type Safety**: Full compile-time validation
- **Performance**: Optimized Dapper usage
- **IntelliSense**: Complete IDE support
- **Error Handling**: Comprehensive exception management
- **Logging**: Built-in query logging

## 🏗️ Implementation Plan

### Step 1: Create Source Generator Project
1. Create `NPA.Generators` project
2. Add necessary NuGet packages
3. Set up project structure
4. Configure build settings

### Step 2: Implement Core Classes
1. Create `RepositoryGenerator` class
2. Create `RepositorySyntaxReceiver` class
3. Create `RepositoryCodeGenerator` class
4. Create `RepositoryTemplate` class

### Step 3: Implement Interface Detection
1. Detect `[Repository]` attributes
2. Analyze interface methods
3. Extract method signatures
4. Validate interface structure

### Step 4: Implement Code Generation
1. Generate repository class
2. Generate constructor
3. Generate method implementations
4. Generate error handling

### Step 5: Add Convention Support
1. Implement naming convention analysis
2. Generate queries from conventions
3. Handle parameter binding
4. Add validation

### Step 6: Create Unit Tests
1. Test interface detection
2. Test code generation
3. Test convention handling
4. Test error scenarios

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Convention guide
4. Best practices

## 📁 File Structure

```
src/NPA.Generators/RepositoryGenerator/
├── RepositoryGenerator.cs
├── RepositorySyntaxReceiver.cs
├── RepositoryCodeGenerator.cs
├── RepositoryTemplate.cs
└── ConventionAnalyzer.cs

tests/NPA.Generators.Tests/RepositoryGenerator/
├── RepositoryGeneratorTests.cs
├── RepositorySyntaxReceiverTests.cs
├── RepositoryCodeGeneratorTests.cs
└── ConventionAnalyzerTests.cs
```

## 💻 Code Examples

### RepositoryGenerator Class
```csharp
[Generator]
public class RepositoryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsRepositoryInterface(node),
                transform: static (ctx, _) => GetRepositoryInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(provider, GenerateRepository);
    }

    private static bool IsRepositoryInterface(SyntaxNode node)
    {
        return node is InterfaceDeclarationSyntax interfaceDecl &&
               interfaceDecl.AttributeLists
                   .SelectMany(al => al.Attributes)
                   .Any(a => a.Name.ToString().Contains("Repository"));
    }

    private static RepositoryInfo? GetRepositoryInfo(GeneratorSyntaxContext context)
    {
        if (context.Node is not InterfaceDeclarationSyntax interfaceDecl)
            return null;

        var semanticModel = context.SemanticModel;
        var symbol = semanticModel.GetDeclaredSymbol(interfaceDecl);
        
        if (symbol == null)
            return null;

        return new RepositoryInfo
        {
            InterfaceName = symbol.Name,
            Namespace = symbol.ContainingNamespace.ToDisplayString(),
            Methods = GetMethods(symbol),
            EntityType = GetEntityType(symbol)
        };
    }

    private static void GenerateRepository(SourceProductionContext context, RepositoryInfo info)
    {
        var code = RepositoryTemplate.Generate(info);
        context.AddSource($"{info.InterfaceName}Implementation.g.cs", code);
    }
}
```

### RepositoryTemplate Class
```csharp
public static class RepositoryTemplate
{
    public static string Generate(RepositoryInfo info)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Dapper;");
        sb.AppendLine();
        
        sb.AppendLine($"namespace {info.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {info.InterfaceName}Implementation : {info.InterfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine("        private readonly IDbConnection _connection;");
        sb.AppendLine();
        sb.AppendLine("        public {info.InterfaceName}Implementation(IDbConnection connection)");
        sb.AppendLine("        {");
        sb.AppendLine("            _connection = connection ?? throw new ArgumentNullException(nameof(connection));");
        sb.AppendLine("        }");
        sb.AppendLine();
        
        foreach (var method in info.Methods)
        {
            sb.AppendLine(GenerateMethod(method, info.EntityType));
        }
        
        sb.AppendLine("    }");
        sb.AppendLine("}");
        
        return sb.ToString();
    }

    private static string GenerateMethod(MethodInfo method, string entityType)
    {
        var methodName = method.Name;
        var returnType = method.ReturnType;
        var parameters = method.Parameters;
        
        // Analyze method name for conventions
        var convention = ConventionAnalyzer.Analyze(methodName);
        
        if (convention != null)
        {
            return GenerateConventionMethod(method, convention, entityType);
        }
        
        // Generate basic method
        return GenerateBasicMethod(method, entityType);
    }

    private static string GenerateConventionMethod(MethodInfo method, ConventionInfo convention, string entityType)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"        public async {method.ReturnType} {method.Name}({string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"))})");
        sb.AppendLine("        {");
        
        // Generate SQL based on convention
        var sql = GenerateSqlFromConvention(convention, entityType);
        var parameters = GenerateParameters(method.Parameters);
        
        sb.AppendLine($"            return await _connection.{GetDapperMethod(method.ReturnType)}({sql}, {parameters});");
        sb.AppendLine("        }");
        
        return sb.ToString();
    }
}
```

### ConventionAnalyzer Class
```csharp
public static class ConventionAnalyzer
{
    public static ConventionInfo? Analyze(string methodName)
    {
        // FindBy{Property}Async
        if (methodName.StartsWith("FindBy") && methodName.EndsWith("Async"))
        {
            var propertyName = methodName.Substring(6, methodName.Length - 11);
            return new ConventionInfo
            {
                Type = ConventionType.FindBy,
                PropertyName = propertyName,
                Operator = "="
            };
        }
        
        // Find{Property}ContainingAsync
        if (methodName.StartsWith("Find") && methodName.Contains("Containing") && methodName.EndsWith("Async"))
        {
            var propertyName = methodName.Substring(4, methodName.IndexOf("Containing") - 4);
            return new ConventionInfo
            {
                Type = ConventionType.FindContaining,
                PropertyName = propertyName,
                Operator = "LIKE"
            };
        }
        
        // Add more conventions...
        
        return null;
    }
}
```

### Example Generated Code
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

        public async Task<User> FindByUsernameAsync(string username)
        {
            return await _connection.QueryFirstOrDefaultAsync<User>(
                "SELECT id, username, email, created_at FROM users WHERE username = @username", 
                new { username });
        }

        public async Task<IEnumerable<User>> FindByEmailDomainAsync(string domain)
        {
            return await _connection.QueryAsync<User>(
                "SELECT id, username, email, created_at FROM users WHERE email LIKE @domain", 
                new { domain = $"%@{domain}" });
        }

        public async Task<IEnumerable<User>> FindActiveUsersAsync()
        {
            return await _connection.QueryAsync<User>(
                "SELECT id, username, email, created_at FROM users WHERE is_active = @active", 
                new { active = true });
        }
    }
}
```

## 🧪 Test Cases

### Interface Detection Tests
- [ ] Detect repository interfaces correctly
- [ ] Handle multiple interfaces
- [ ] Handle nested interfaces
- [ ] Handle generic interfaces
- [ ] Handle invalid interfaces

### Code Generation Tests
- [ ] Generate basic repository class
- [ ] Generate constructor correctly
- [ ] Generate method implementations
- [ ] Handle different return types
- [ ] Handle different parameter types

### Convention Analysis Tests
- [ ] Analyze FindBy conventions
- [ ] Analyze FindContaining conventions
- [ ] Analyze FindStartingWith conventions
- [ ] Analyze FindEndingWith conventions
- [ ] Analyze comparison conventions

### Generated Code Tests
- [ ] Generated code compiles
- [ ] Generated code works correctly
- [ ] Generated code is type-safe
- [ ] Generated code is performant
- [ ] Generated code handles errors

### Error Handling Tests
- [ ] Handle invalid method signatures
- [ ] Handle missing attributes
- [ ] Handle compilation errors
- [ ] Handle runtime errors
- [ ] Handle null parameters

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Basic repository generation
- [ ] Convention-based methods
- [ ] Custom method implementation
- [ ] Best practices
- [ ] Error handling

### Convention Guide
- [ ] Supported naming conventions
- [ ] Method signature requirements
- [ ] Parameter binding
- [ ] Return type handling
- [ ] Examples for each convention

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
1. Move to Phase 2.1: Relationship Mapping
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on convention patterns
- [ ] Performance considerations for code generation
- [ ] Integration with existing Dapper features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
