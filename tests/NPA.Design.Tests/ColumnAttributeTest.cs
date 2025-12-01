using Xunit;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System.Threading.Tasks;

namespace NPA.Design.Tests;

public class ColumnAttributeTest
{

    [Fact]
    public void CanReadColumnAttributeFromSource()
    {
        // Include Column attribute source so Roslyn can read constructor arguments
        var columnAttributeSource = @"
namespace NPA.Core.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class ColumnAttribute : System.Attribute
    {
        public string Name { get; }
        public ColumnAttribute(string name) { Name = name; }
    }
}";

        var source = @"
using NPA.Core.Annotations;

namespace Test
{
    public class User
    {
        [Column(""email"")]
        public string Email { get; set; }
    }
}";
        
        // Create compilation with BOTH attribute definition and usage
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(columnAttributeSource),
            CSharpSyntaxTree.ParseText(source)
        };
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        
        var userClass = compilation.GetTypeByMetadataName("Test.User");
        userClass.Should().NotBeNull();
        
        var emailProp = userClass!.GetMembers("Email").OfType<IPropertySymbol>().FirstOrDefault();
        emailProp.Should().NotBeNull();
        
        var attrs = emailProp!.GetAttributes();
        attrs.Should().NotBeEmpty();
        
        var columnAttr = attrs.FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute");
        columnAttr.Should().NotBeNull($"Column attribute should be found");
        
        var args = columnAttr!.ConstructorArguments;
        args.Should().NotBeEmpty("Attribute should have constructor arguments");
        
        var value = args[0].Value;
        value.Should().Be("email", "Attribute value should be 'email'");
    }
}
