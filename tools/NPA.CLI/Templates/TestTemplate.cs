namespace NPA.CLI.Templates;

/// <summary>
/// Template for generating test classes.
/// </summary>
public class TestTemplate
{
    public string Generate(string className, string namespaceName)
    {
        return $@"using Xunit;
using NPA.Core;
using {namespaceName.Replace(".Tests", "")};

namespace {namespaceName};

/// <summary>
/// Unit tests for {className}.
/// </summary>
public class {className}Tests
{{
    [Fact]
    public void Test_{className}_Creation()
    {{
        // Arrange
        
        // Act
        
        // Assert
        Assert.True(true, ""Test not implemented"");
    }}

    [Fact]
    public async Task Test_{className}_AsyncOperation()
    {{
        // Arrange
        
        // Act
        
        // Assert
        await Task.CompletedTask;
        Assert.True(true, ""Test not implemented"");
    }}

    // Add more test methods here
}}
";
    }
}
