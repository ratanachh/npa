using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using FluentAssertions;
using NPA.Generators.Analyzers;
using Xunit;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Loader;

namespace NPA.Generators.Tests.Analyzers;

public class RepositoryGenerationAnalyzerTests
{
    [Fact]
    public async Task MissingPartialKeyword_ReportsDiagnostic()
    {
        var code = @"
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class GenerateRepositoryAttribute : System.Attribute
    {
        public GenerateRepositoryAttribute(System.Type entityType) { }
    }

    [GenerateRepository(typeof(User))]
    public class UserRepository : IRepository<User>
    {
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics.Single();
        diagnostic.Id.Should().Be(RepositoryGenerationAnalyzer.MissingPartialKeywordId);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task PartialKeywordPresent_NoDiagnostic()
    {
        var code = @"
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class GenerateRepositoryAttribute : System.Attribute
    {
        public GenerateRepositoryAttribute(System.Type entityType) { }
    }

    [GenerateRepository(typeof(User))]
    public partial class UserRepository : IRepository<User>
    {
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task InvalidEntityType_Interface_ReportsDiagnostic()
    {
        var code = @"
using NPA.Core.Repositories;

namespace TestNamespace
{
    public interface IUser
    {
        int Id { get; set; }
        string Name { get; set; }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class GenerateRepositoryAttribute : System.Attribute
    {
        public GenerateRepositoryAttribute(System.Type entityType) { }
    }

    [GenerateRepository(typeof(IUser))]
    public partial class UserRepository : IRepository<IUser>
    {
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        
        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics.Single();
        diagnostic.Id.Should().Be(RepositoryGenerationAnalyzer.InvalidEntityTypeId);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task NoGenerateRepositoryAttribute_NoDiagnostic()
    {
        var code = @"
using NPA.Core.Repositories;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class UserRepository : IRepository<User>
    {
    }
}";

        var diagnostics = await GetDiagnosticsAsync(code);
        diagnostics.Should().BeEmpty();
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        
        // Get the runtime directory for system references
        var runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(typeof(NPA.Core.Repositories.IRepository<>).Assembly.Location)
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new RepositoryGenerationAnalyzer()));

        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diagnostics;
    }
}
