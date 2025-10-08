using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NPA.Generators;
using Xunit;

namespace NPA.Generators.Tests;

public class RepositoryGeneratorTests
{
    [Fact]
    public void Execute_ShouldGenerateRepositoryClass()
    {
        // Arrange
        var generator = new RepositoryGenerator();
        var compilation = CreateCompilation(@"
public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; }
}");

        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        // Assert
        diagnostics.Should().BeEmpty();
        outputCompilation.SyntaxTrees.Should().HaveCountGreaterThan(compilation.SyntaxTrees.Count());
    }

    private static Compilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("TestAssembly",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}