# Phase 6.1: Visual Studio Extension

## 📋 Task Overview

**Objective**: Create a Visual Studio extension that provides IntelliSense support, code generation tools, and project templates for the NPA library.

**Priority**: Low  
**Estimated Time**: 5-6 days  
**Dependencies**: Phase 1.1-1.5, Phase 2.1-2.6, Phase 3.1-3.5, Phase 4.1-4.7, Phase 5.1-5.5 (All previous phases)  
**Assigned To**: [Developer Name]  

## 🎯 Success Criteria

- [ ] Visual Studio extension project is created
- [ ] IntelliSense support works
- [ ] Code generation tools work
- [ ] Project templates work
- [ ] Unit tests cover all functionality
- [ ] Documentation is complete

## 📝 Detailed Requirements

### 1. Visual Studio Extension Features
- **IntelliSense Support**: Auto-completion for NPA attributes and methods
- **Code Generation**: Generate repositories and entities from templates
- **Project Templates**: NPA project templates for different scenarios
- **Syntax Highlighting**: Highlight NPA-specific syntax
- **Error Detection**: Detect and highlight NPA-related errors
- **Quick Actions**: Refactoring and code generation actions

### 2. IntelliSense Support
- **Attribute Completion**: Auto-complete NPA attributes
- **Method Completion**: Auto-complete repository methods
- **Parameter Completion**: Auto-complete method parameters
- **Documentation**: Show XML documentation for NPA types
- **Snippets**: Code snippets for common NPA patterns

### 3. Code Generation Tools
- **Repository Generator**: Generate repository interfaces and implementations
- **Entity Generator**: Generate entity classes from database schema
- **Migration Generator**: Generate migration classes
- **Configuration Generator**: Generate NPA configuration

### 4. Project Templates
- **NPA Console App**: Console application with NPA
- **NPA Web API**: Web API with NPA
- **NPA Blazor App**: Blazor application with NPA
- **NPA Class Library**: Class library with NPA

### 5. Visual Studio Integration
- **Solution Explorer**: NPA-specific project items
- **Properties Window**: NPA configuration properties
- **Error List**: NPA-specific errors and warnings
- **Output Window**: NPA build and generation output

## 🏗️ Implementation Plan

### Step 1: Create Extension Project
1. Create Visual Studio extension project
2. Set up project structure
3. Configure extension manifest
4. Add necessary NuGet packages

### Step 2: Implement IntelliSense Support
1. Create completion source provider
2. Implement attribute completion
3. Implement method completion
4. Add documentation support

### Step 3: Implement Code Generation
1. Create code generation service
2. Implement repository generator
3. Implement entity generator
4. Implement migration generator

### Step 4: Create Project Templates
1. Create project template structure
2. Implement console app template
3. Implement web API template
4. Implement Blazor app template

### Step 5: Add Visual Studio Integration
1. Implement solution explorer integration
2. Add properties window support
3. Implement error list integration
4. Add output window support

### Step 6: Create Unit Tests
1. Test IntelliSense functionality
2. Test code generation
3. Test project templates
4. Test Visual Studio integration

### Step 7: Add Documentation
1. XML documentation comments
2. Usage examples
3. Extension guide
4. Best practices

## 📁 File Structure

```
src/NPA.VisualStudio/
├── NPA.VisualStudio.csproj
├── source.extension.vsixmanifest
├── Commands/
│   ├── GenerateRepositoryCommand.cs
│   ├── GenerateEntityCommand.cs
│   └── GenerateMigrationCommand.cs
├── IntelliSense/
│   ├── NPACompletionSource.cs
│   ├── NPADocumentationProvider.cs
│   └── NPASnippetProvider.cs
├── Templates/
│   ├── NPAConsoleAppTemplate.cs
│   ├── NPAWebAPITemplate.cs
│   ├── NPABlazorAppTemplate.cs
│   └── NPAClassLibraryTemplate.cs
├── Services/
│   ├── ICodeGenerationService.cs
│   ├── CodeGenerationService.cs
│   ├── IProjectTemplateService.cs
│   └── ProjectTemplateService.cs
├── UI/
│   ├── GenerateRepositoryDialog.xaml
│   ├── GenerateEntityDialog.xaml
│   └── NPASettingsDialog.xaml
└── Properties/
    ├── AssemblyInfo.cs
    └── Resources.resx

tests/NPA.VisualStudio.Tests/
├── Commands/
│   ├── GenerateRepositoryCommandTests.cs
│   ├── GenerateEntityCommandTests.cs
│   └── GenerateMigrationCommandTests.cs
├── IntelliSense/
│   ├── NPACompletionSourceTests.cs
│   ├── NPADocumentationProviderTests.cs
│   └── NPASnippetProviderTests.cs
├── Templates/
│   ├── NPAConsoleAppTemplateTests.cs
│   ├── NPAWebAPITemplateTests.cs
│   └── NPABlazorAppTemplateTests.cs
└── Services/
    ├── CodeGenerationServiceTests.cs
    └── ProjectTemplateServiceTests.cs
```

## 💻 Code Examples

### Extension Manifest
```xml
<?xml version="1.0" encoding="utf-8"?>
<PackageManifest Version="2.0.0" xmlns="http://schemas.microsoft.com/developer/vsx-schema/2011" xmlns:d="http://schemas.microsoft.com/developer/vsx-schema-design/2011">
  <Metadata>
    <Identity Id="NPA.VisualStudio.12345678-1234-1234-1234-123456789012" Version="1.0.0" Language="en-US" Publisher="NPA Team" />
    <DisplayName>NPA Visual Studio Extension</DisplayName>
    <Description>Visual Studio extension for NPA (JPA-like ORM for .NET)</Description>
    <MoreInfo>https://github.com/npa/npa</MoreInfo>
    <License>LICENSE.txt</License>
    <GettingStartedGuide>https://github.com/npa/npa/wiki/Getting-Started</GettingStartedGuide>
    <ReleaseNotes>https://github.com/npa/npa/releases</ReleaseNotes>
    <Icon>Resources\icon.png</Icon>
    <PreviewImage>Resources\preview.png</PreviewImage>
    <Tags>ORM, Database, Entity Framework, Dapper, JPA</Tags>
  </Metadata>
  <Installation>
    <InstallationTarget Id="Microsoft.VisualStudio.Community" Version="[17.0,18.0)" />
    <InstallationTarget Id="Microsoft.VisualStudio.Professional" Version="[17.0,18.0)" />
    <InstallationTarget Id="Microsoft.VisualStudio.Enterprise" Version="[17.0,18.0)" />
  </Installation>
  <Dependencies>
    <Dependency Id="Microsoft.Framework.NDP" DisplayName="Microsoft .NET Framework" d:Source="Manual" Version="[4.7.2,)" />
  </Dependencies>
  <Assets>
    <Asset Type="Microsoft.VisualStudio.VsPackage" d:Source="Project" d:ProjectName="NPA.VisualStudio" Path="|%CurrentProject%;PkgdefProjectOutputGroup|" />
    <Asset Type="Microsoft.VisualStudio.MefComponent" d:Source="Project" d:ProjectName="NPA.VisualStudio" Path="|%CurrentProject%|" />
  </Assets>
</PackageManifest>
```

### Completion Source Provider
```csharp
[Export(typeof(ICompletionSourceProvider))]
[Name("NPA Completion Source Provider")]
[ContentType("csharp")]
[Order(After = "default")]
public class NPACompletionSourceProvider : ICompletionSourceProvider
{
    [Import]
    public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }
    
    public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
    {
        return new NPACompletionSource(textBuffer, TextDocumentFactoryService);
    }
}

public class NPACompletionSource : ICompletionSource
{
    private readonly ITextBuffer _textBuffer;
    private readonly ITextDocumentFactoryService _textDocumentFactoryService;
    private bool _disposed = false;
    
    public NPACompletionSource(ITextBuffer textBuffer, ITextDocumentFactoryService textDocumentFactoryService)
    {
        _textBuffer = textBuffer;
        _textDocumentFactoryService = textDocumentFactoryService;
    }
    
    public void AugmentCompletionSession(ICompletionSession session, IList<CompletionSet> completionSets)
    {
        if (_disposed)
            return;
        
        var triggerPoint = session.GetTriggerPoint(_textBuffer);
        if (triggerPoint == null)
            return;
        
        var line = triggerPoint.Value.GetContainingLine();
        var text = line.GetText();
        var position = triggerPoint.Value.Position - line.Start.Position;
        
        var completions = GetCompletions(text, position);
        if (completions.Any())
        {
            var applicableSpan = GetApplicableSpan(triggerPoint.Value);
            var completionSet = new CompletionSet(
                "NPA",
                "NPA",
                applicableSpan,
                completions,
                null);
            
            completionSets.Add(completionSet);
        }
    }
    
    private List<Completion> GetCompletions(string text, int position)
    {
        var completions = new List<Completion>();
        
        // Check if we're in an attribute context
        if (IsInAttributeContext(text, position))
        {
            completions.AddRange(GetAttributeCompletions());
        }
        
        // Check if we're in a method context
        if (IsInMethodContext(text, position))
        {
            completions.AddRange(GetMethodCompletions());
        }
        
        return completions;
    }
    
    private bool IsInAttributeContext(string text, int position)
    {
        var beforeCursor = text.Substring(0, position);
        return beforeCursor.Contains("[") && !beforeCursor.Contains("]");
    }
    
    private bool IsInMethodContext(string text, int position)
    {
        var beforeCursor = text.Substring(0, position);
        return beforeCursor.Contains("Task<") || beforeCursor.Contains("async");
    }
    
    private List<Completion> GetAttributeCompletions()
    {
        return new List<Completion>
        {
            new Completion("Entity", "Entity", "Marks a class as an entity", null, null),
            new Completion("Table", "Table", "Specifies the database table name", null, null),
            new Completion("Id", "Id", "Marks a property as primary key", null, null),
            new Completion("Column", "Column", "Maps property to database column", null, null),
            new Completion("OneToMany", "OneToMany", "Maps one-to-many relationship", null, null),
            new Completion("ManyToOne", "ManyToOne", "Maps many-to-one relationship", null, null),
            new Completion("ManyToMany", "ManyToMany", "Maps many-to-many relationship", null, null),
            new Completion("Repository", "Repository", "Marks interface as repository", null, null)
        };
    }
    
    private List<Completion> GetMethodCompletions()
    {
        return new List<Completion>
        {
            new Completion("FindBy", "FindBy", "Find entity by property", null, null),
            new Completion("FindAll", "FindAll", "Find all entities", null, null),
            new Completion("Create", "Create", "Create new entity", null, null),
            new Completion("Update", "Update", "Update existing entity", null, null),
            new Completion("Delete", "Delete", "Delete entity", null, null),
            new Completion("Count", "Count", "Count entities", null, null),
            new Completion("Exists", "Exists", "Check if entity exists", null, null)
        };
    }
    
    private ITrackingSpan GetApplicableSpan(ITrackingPoint triggerPoint)
    {
        var line = triggerPoint.GetContainingLine();
        var text = line.GetText();
        var position = triggerPoint.Position - line.Start.Position;
        
        var start = position;
        while (start > 0 && char.IsLetterOrDigit(text[start - 1]))
        {
            start--;
        }
        
        var end = position;
        while (end < text.Length && char.IsLetterOrDigit(text[end]))
        {
            end++;
        }
        
        return _textBuffer.CreateTrackingSpan(
            line.Start + start,
            end - start,
            SpanTrackingMode.EdgeInclusive);
    }
    
    public void Dispose()
    {
        _disposed = true;
    }
}
```

### Code Generation Service
```csharp
[Export(typeof(ICodeGenerationService))]
public class CodeGenerationService : ICodeGenerationService
{
    [Import]
    public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }
    
    [Import]
    public IProjectService ProjectService { get; set; }
    
    public async Task GenerateRepositoryAsync(ITextBuffer textBuffer, string entityName, string namespaceName)
    {
        var repositoryInterface = GenerateRepositoryInterface(entityName, namespaceName);
        var repositoryImplementation = GenerateRepositoryImplementation(entityName, namespaceName);
        
        // Insert repository interface
        var interfaceSpan = new SnapshotSpan(textBuffer.CurrentSnapshot, 0, 0);
        textBuffer.Insert(0, repositoryInterface);
        
        // Insert repository implementation
        var implementationSpan = new SnapshotSpan(textBuffer.CurrentSnapshot, repositoryInterface.Length, 0);
        textBuffer.Insert(repositoryInterface.Length, repositoryImplementation);
    }
    
    public async Task GenerateEntityAsync(ITextBuffer textBuffer, string tableName, string namespaceName)
    {
        var entity = GenerateEntityClass(tableName, namespaceName);
        
        var span = new SnapshotSpan(textBuffer.CurrentSnapshot, 0, 0);
        textBuffer.Insert(0, entity);
    }
    
    public async Task GenerateMigrationAsync(ITextBuffer textBuffer, string migrationName, string description)
    {
        var migration = GenerateMigrationClass(migrationName, description);
        
        var span = new SnapshotSpan(textBuffer.CurrentSnapshot, 0, 0);
        textBuffer.Insert(0, migration);
    }
    
    private string GenerateRepositoryInterface(string entityName, string namespaceName)
    {
        return $@"using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core;

namespace {namespaceName}
{{
    public interface I{entityName}Repository : IRepository<{entityName}, long>
    {{
        Task<{entityName}> FindByUsernameAsync(string username);
        Task<IEnumerable<{entityName}>> FindActiveAsync();
        Task<int> CountAsync();
    }}
}}";
    }
    
    private string GenerateRepositoryImplementation(string entityName, string namespaceName)
    {
        return $@"using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace {namespaceName}
{{
    public partial class {entityName}Repository : I{entityName}Repository
    {{
        private readonly IDbConnection _connection;
        
        public {entityName}Repository(IDbConnection connection)
        {{
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }}
        
        public async Task<{entityName}> FindByUsernameAsync(string username)
        {{
            return await _connection.QueryFirstOrDefaultAsync<{entityName}>(
                ""SELECT * FROM {entityName.ToLower()}s WHERE username = @username"", 
                new {{ username }});
        }}
        
        public async Task<IEnumerable<{entityName}>> FindActiveAsync()
        {{
            return await _connection.QueryAsync<{entityName}>(
                ""SELECT * FROM {entityName.ToLower()}s WHERE is_active = @active"", 
                new {{ active = true }});
        }}
        
        public async Task<int> CountAsync()
        {{
            return await _connection.QuerySingleAsync<int>(
                ""SELECT COUNT(*) FROM {entityName.ToLower()}s"");
        }}
    }}
}}";
    }
    
    private string GenerateEntityClass(string tableName, string namespaceName)
    {
        return $@"using System;
using NPA.Core;

namespace {namespaceName}
{{
    [Entity]
    [Table(""{tableName}"")]
    public class {tableName}
    {{
        [Id]
        [GeneratedValue(GenerationType.Identity)]
        public long Id {{ get; set; }}
        
        [Column(""name"")]
        public string Name {{ get; set; }}
        
        [Column(""created_at"")]
        public DateTime CreatedAt {{ get; set; }}
    }}
}}";
    }
    
    private string GenerateMigrationClass(string migrationName, string description)
    {
        return $@"using System;
using System.Data;
using System.Threading.Tasks;
using NPA.Core.Migrations;

namespace Migrations
{{
    public class {migrationName} : Migration
    {{
        public override string Name => ""{migrationName}"";
        public override int Version => {DateTime.Now:yyyyMMddHH};
        public override DateTime CreatedAt => DateTime.UtcNow;
        public override string Description => ""{description}"";
        
        public override async Task UpAsync(IDbConnection connection)
        {{
            // Add your migration SQL here
        }}
        
        public override async Task DownAsync(IDbConnection connection)
        {{
            // Add your rollback SQL here
        }}
    }}
}}";
    }
}
```

### Project Template
```csharp
[Export(typeof(IProjectTemplate))]
public class NPAConsoleAppTemplate : IProjectTemplate
{
    public string Name => "NPA Console Application";
    public string Description => "A console application with NPA ORM";
    public string DefaultProjectName => "NPAConsoleApp";
    
    public async Task<Project> CreateProjectAsync(string projectPath, string projectName)
    {
        var project = new Project
        {
            Name = projectName,
            Path = projectPath,
            Type = ProjectType.ConsoleApplication
        };
        
        // Add project files
        await AddProjectFileAsync(project, "Program.cs", GenerateProgramCs());
        await AddProjectFileAsync(project, "User.cs", GenerateUserEntity());
        await AddProjectFileAsync(project, "IUserRepository.cs", GenerateUserRepositoryInterface());
        await AddProjectFileAsync(project, "UserRepository.cs", GenerateUserRepositoryImplementation());
        await AddProjectFileAsync(project, "appsettings.json", GenerateAppSettings());
        await AddProjectFileAsync(project, "NPAConsoleApp.csproj", GenerateProjectFile());
        
        return project;
    }
    
    private string GenerateProgramCs()
    {
        return @"using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using NPA.Core;
using NPA.Extensions;

namespace NPAConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            using (var scope = host.Services.CreateScope())
            {
                var entityManager = scope.ServiceProvider.GetRequiredService<IEntityManager>();
                
                // Your application logic here
                Console.WriteLine(""Hello NPA!"");
            }
        }
        
        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddNPA(config =>
                    {
                        config.ConnectionString = context.Configuration.GetConnectionString(""DefaultConnection"");
                        config.DatabaseProvider = DatabaseProvider.SqlServer;
                    });
                });
    }
}";
    }
    
    private string GenerateUserEntity()
    {
        return @"using System;
using NPA.Core;

namespace NPAConsoleApp
{
    [Entity]
    [Table(""users"")]
    public class User
    {
        [Id]
        [GeneratedValue(GenerationType.Identity)]
        public long Id { get; set; }
        
        [Column(""username"")]
        public string Username { get; set; }
        
        [Column(""email"")]
        public string Email { get; set; }
        
        [Column(""created_at"")]
        public DateTime CreatedAt { get; set; }
    }
}";
    }
    
    private string GenerateUserRepositoryInterface()
    {
        return @"using System.Collections.Generic;
using System.Threading.Tasks;
using NPA.Core;

namespace NPAConsoleApp
{
    public interface IUserRepository : IRepository<User, long>
    {
        Task<User> FindByUsernameAsync(string username);
        Task<IEnumerable<User>> FindActiveAsync();
    }
}";
    }
    
    private string GenerateUserRepositoryImplementation()
    {
        return @"using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;

namespace NPAConsoleApp
{
    public partial class UserRepository : IUserRepository
    {
        private readonly IDbConnection _connection;
        
        public UserRepository(IDbConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        
        public async Task<User> FindByUsernameAsync(string username)
        {
            return await _connection.QueryFirstOrDefaultAsync<User>(
                ""SELECT * FROM users WHERE username = @username"", 
                new { username });
        }
        
        public async Task<IEnumerable<User>> FindActiveAsync()
        {
            return await _connection.QueryAsync<User>(
                ""SELECT * FROM users WHERE is_active = @active"", 
                new { active = true });
        }
    }
}";
    }
    
    private string GenerateAppSettings()
    {
        return @"{
  ""ConnectionStrings"": {
    ""DefaultConnection"": ""Server=localhost;Database=NPAConsoleApp;Trusted_Connection=true;""
  },
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft"": ""Warning"",
      ""Microsoft.Hosting.Lifetime"": ""Information""
    }
  }
}";
    }
    
    private string GenerateProjectFile()
    {
        return @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net6.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include=""NPA"" Version=""1.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Hosting"" Version=""6.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Configuration.Json"" Version=""6.0.0"" />
  </ItemGroup>
</Project>";
    }
}
```

## 🧪 Test Cases

### IntelliSense Tests
- [ ] Attribute completion
- [ ] Method completion
- [ ] Parameter completion
- [ ] Documentation display
- [ ] Snippet functionality

### Code Generation Tests
- [ ] Repository generation
- [ ] Entity generation
- [ ] Migration generation
- [ ] Configuration generation
- [ ] Error handling

### Project Template Tests
- [ ] Console app template
- [ ] Web API template
- [ ] Blazor app template
- [ ] Class library template
- [ ] Template customization

### Visual Studio Integration Tests
- [ ] Solution explorer integration
- [ ] Properties window support
- [ ] Error list integration
- [ ] Output window support
- [ ] Menu integration

### Extension Tests
- [ ] Extension loading
- [ ] Command execution
- [ ] UI interaction
- [ ] Performance
- [ ] Error handling

## 📚 Documentation Requirements

### XML Documentation
- [ ] All public members documented
- [ ] Parameter descriptions
- [ ] Return value descriptions
- [ ] Exception documentation
- [ ] Usage examples

### Usage Guide
- [ ] Extension installation
- [ ] IntelliSense usage
- [ ] Code generation
- [ ] Project templates
- [ ] Best practices

### Extension Guide
- [ ] Features overview
- [ ] Configuration
- [ ] Troubleshooting
- [ ] Customization
- [ ] Advanced usage

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
1. Move to Phase 6.2: Code Generation Tools
2. Update checklist with completion status
3. Create pull request for review
4. Update documentation

## 📞 Questions/Issues

- [ ] Clarification needed on Visual Studio integration
- [ ] Performance considerations for IntelliSense
- [ ] Integration with existing features
- [ ] Error message localization

---

*Created: [Current Date]*  
*Last Updated: [Current Date]*  
*Status: In Progress*
