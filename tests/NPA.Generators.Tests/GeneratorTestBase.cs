using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NPA.Core.Annotations;

namespace NPA.Generators.Tests;

/// <summary>
/// Base class for source generator tests with shared helper methods.
/// </summary>
public abstract class GeneratorTestBase
{
    /// <summary>
    /// Common NPA annotation source code that needs to be included for proper attribute parsing.
    /// This allows Roslyn to read attribute constructor arguments during source generation.
    /// </summary>
    protected const string NPA_ANNOTATIONS_SOURCE = @"
namespace NPA.Core.Annotations
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class ColumnAttribute : System.Attribute
    {
        public string Name { get; }
        public string? TypeName { get; set; }
        public int? Length { get; set; }
        public int? Precision { get; set; }
        public int? Scale { get; set; }
        public bool IsNullable { get; set; } = true;
        public bool IsUnique { get; set; } = false;
        public ColumnAttribute(string name) { Name = name; }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class TableAttribute : System.Attribute
    {
        public string Name { get; }
        public string? Schema { get; set; }
        public TableAttribute(string name) { Name = name; }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class IdAttribute : System.Attribute { }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class RequiredAttribute : System.Attribute { }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class UniqueAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public sealed class EntityAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true)]
    public sealed class NamedQueryAttribute : System.Attribute
    {
        public string Name { get; }
        public string Query { get; }
        public bool NativeQuery { get; set; }
        public int? CommandTimeout { get; set; }
        public bool Buffered { get; set; } = true;
        public string? Description { get; set; }
        
        public NamedQueryAttribute(string name, string query) 
        { 
            Name = name;
            Query = query;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class QueryAttribute : System.Attribute
    {
        public string Sql { get; }
        public bool NativeQuery { get; set; }
        public int? CommandTimeout { get; set; }
        public bool Buffered { get; set; } = true;
        
        public QueryAttribute(string sql) 
        { 
            Sql = sql;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Interface)]
    public sealed class RepositoryAttribute : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class GeneratedValueAttribute : System.Attribute { }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class OneToOneAttribute : System.Attribute
    {
        public string? MappedBy { get; set; }
        public FetchType Fetch { get; set; } = FetchType.Eager;
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class OneToManyAttribute : System.Attribute
    {
        public string? MappedBy { get; set; }
        public CascadeType Cascade { get; set; } = CascadeType.None;
        public FetchType Fetch { get; set; } = FetchType.Lazy;
        public bool OrphanRemoval { get; set; } = false;
        
        public OneToManyAttribute() { }
        public OneToManyAttribute(string mappedBy) { MappedBy = mappedBy; }
    }
    
    public enum FetchType
    {
        Eager = 0,
        Lazy = 1
    }
    
    [System.Flags]
    public enum CascadeType
    {
        None = 0,
        Persist = 1 << 0,  // 1
        Merge = 1 << 1,    // 2
        Remove = 1 << 2,   // 4
        Refresh = 1 << 3,  // 8
        Detach = 1 << 4,   // 16
        All = Persist | Merge | Remove | Refresh | Detach  // 31
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class ManyToOneAttribute : System.Attribute
    {
        public CascadeType Cascade { get; set; } = CascadeType.None;
        public FetchType Fetch { get; set; } = FetchType.Eager;
        public bool Optional { get; set; } = true;
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class ManyToManyAttribute : System.Attribute
    {
        public string? MappedBy { get; set; }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false)]
    public sealed class JoinColumnAttribute : System.Attribute
    {
        public string Name { get; }
        public string? ReferencedColumnName { get; set; }
        public bool Nullable { get; set; } = true;
        public bool Unique { get; set; } = false;
        public bool Insertable { get; set; } = true;
        public bool Updatable { get; set; } = true;
        public JoinColumnAttribute(string name) { Name = name; }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class JoinTableAttribute : System.Attribute
    {
        public string Name { get; }
        public string? Schema { get; set; }
        public string[]? JoinColumns { get; set; }
        public string[]? InverseJoinColumns { get; set; }
        public JoinTableAttribute(string name) { Name = name; }
    }
}";

    /// <summary>
    /// Creates a CSharpCompilation from source code with all necessary references.
    /// </summary>
    /// <param name="source">The source code to compile.</param>
    /// <param name="includeAnnotationSource">Whether to include the NPA annotation definitions.</param>
    /// <returns>A CSharpCompilation ready for generator testing.</returns>
    protected static Compilation CreateCompilation(string source, bool includeAnnotationSource = true)
    {
        var syntaxTrees = includeAnnotationSource
            ? new[] { CSharpSyntaxTree.ParseText(NPA_ANNOTATIONS_SOURCE), CSharpSyntaxTree.ParseText(source) }
            : new[] { CSharpSyntaxTree.ParseText(source) };
        
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RepositoryAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(NPA.Core.Repositories.IRepository<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Runs a source generator and returns the generation result.
    /// </summary>
    /// <typeparam name="TGenerator">The type of generator to run.</typeparam>
    /// <param name="source">The source code to process.</param>
    /// <param name="includeAnnotationSource">Whether to include the NPA annotation definitions.</param>
    /// <returns>The generator run result.</returns>
    protected static GeneratorRunResult RunGenerator<TGenerator>(string source, bool includeAnnotationSource = true)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation(source, includeAnnotationSource);
        var generator = new TGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out _, 
            out _);

        var runResult = driver.GetRunResult();
        return runResult.Results[0];
    }

    /// <summary>
    /// Runs a generator and returns the output compilation and diagnostics.
    /// </summary>
    /// <typeparam name="TGenerator">The type of generator to run.</typeparam>
    /// <param name="source">The source code to process.</param>
    /// <param name="outputCompilation">The output compilation after generator runs.</param>
    /// <param name="diagnostics">Any diagnostics produced during generation.</param>
    /// <param name="includeAnnotationSource">Whether to include the NPA annotation definitions.</param>
    protected static void RunGeneratorWithOutput<TGenerator>(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        bool includeAnnotationSource = true)
        where TGenerator : IIncrementalGenerator, new()
    {
        var compilation = CreateCompilation(source, includeAnnotationSource);
        var generator = new TGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        
        driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(
            compilation, 
            out outputCompilation, 
            out diagnostics);
    }

    /// <summary>
    /// Extracts the generated source code from a generator run result.
    /// </summary>
    /// <param name="result">The generator run result.</param>
    /// <param name="index">The index of the generated source (default: 0).</param>
    /// <returns>The generated source code as a string, or empty string if no sources generated.</returns>
    protected static string GetGeneratedCode(GeneratorRunResult result, int index = 0)
    {
        if (result.GeneratedSources.Length == 0 || index >= result.GeneratedSources.Length)
            return string.Empty;
            
        return result.GeneratedSources[index].SourceText.ToString();
    }

    /// <summary>
    /// Finds generated code by searching for a specific pattern in the file path.
    /// </summary>
    /// <param name="outputCompilation">The output compilation from generator.</param>
    /// <param name="filePathPattern">The pattern to search for in file paths.</param>
    /// <returns>The first matching generated source code, or empty string if not found.</returns>
    protected static string FindGeneratedCode(Compilation outputCompilation, string filePathPattern)
    {
        var tree = outputCompilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains(filePathPattern));
            
        return tree?.ToString() ?? string.Empty;
    }
}
