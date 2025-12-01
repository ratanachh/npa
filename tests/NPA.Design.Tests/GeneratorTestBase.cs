using System.Collections.Immutable;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NPA.Core.Annotations;
using NPA.Design.Generators;
using FluentAssertions;

namespace NPA.Design.Tests;

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
        public CascadeType Cascade { get; set; } = CascadeType.None;
        public FetchType Fetch { get; set; } = FetchType.Eager;
        public bool Optional { get; set; } = true;
        public bool OrphanRemoval { get; set; } = false;
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class OneToManyAttribute : System.Attribute
    {
        public string MappedBy { get; set; } = string.Empty;
        public CascadeType Cascade { get; set; } = CascadeType.None;
        public FetchType Fetch { get; set; } = FetchType.Lazy;
        public bool OrphanRemoval { get; set; } = false;
        
        public OneToManyAttribute() { }
        public OneToManyAttribute(string mappedBy) { MappedBy = mappedBy ?? throw new System.ArgumentNullException(nameof(mappedBy)); }
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
        public string MappedBy { get; set; } = string.Empty;
        public CascadeType Cascade { get; set; } = CascadeType.None;
        public FetchType Fetch { get; set; } = FetchType.Lazy;
        public bool OrphanRemoval { get; set; } = false;
        
        public ManyToManyAttribute() { }
        public ManyToManyAttribute(string mappedBy) { MappedBy = mappedBy ?? throw new System.ArgumentNullException(nameof(mappedBy)); }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public sealed class JoinColumnAttribute : System.Attribute
    {
        public string Name { get; set; } = string.Empty;
        public string ReferencedColumnName { get; set; } = ""id"";
        public bool Nullable { get; set; } = true;
        public bool Unique { get; set; } = false;
        public bool Insertable { get; set; } = true;
        public bool Updatable { get; set; } = true;
        public JoinColumnAttribute() { }
        public JoinColumnAttribute(string name) { Name = name ?? throw new System.ArgumentNullException(nameof(name)); }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class JoinTableAttribute : System.Attribute
    {
        public string Name { get; set; } = string.Empty;
        public string Schema { get; set; } = string.Empty;
        public string[] JoinColumns { get; set; } = System.Array.Empty<string>();
        public string[] InverseJoinColumns { get; set; } = System.Array.Empty<string>();
        public JoinTableAttribute() { }
        public JoinTableAttribute(string name) { Name = name ?? throw new System.ArgumentNullException(nameof(name)); }
    }
    
    [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class MultiTenantAttribute : System.Attribute
    {
        public string TenantIdProperty { get; }
        public bool EnforceTenantIsolation { get; set; } = true;
        public bool AllowCrossTenantQueries { get; set; } = false;
        public bool ValidateTenantOnWrite { get; set; } = true;
        public bool AutoPopulateTenantId { get; set; } = true;
        
        public MultiTenantAttribute(string tenantIdProperty = ""TenantId"")
        {
            TenantIdProperty = tenantIdProperty;
        }
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
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RepositoryAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Core.Repositories.IRepository<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
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

    /// <summary>
    /// Gets the NPA annotations source code for use in integration tests.
    /// </summary>
    /// <returns>The complete NPA annotations source code.</returns>
    public static string GetNpaAnnotationsSource() => NPA_ANNOTATIONS_SOURCE;

    /// <summary>
    /// Creates a compilation from multiple source files with all necessary references.
    /// Extended version for integration tests that need multiple source files.
    /// </summary>
    /// <param name="sources">Array of source code strings to include in compilation.</param>
    /// <param name="includeAnnotationSource">Whether to include the NPA annotation definitions.</param>
    /// <param name="additionalReferences">Additional metadata references to include.</param>
    /// <returns>A CSharpCompilation ready for generator testing.</returns>
    public static Compilation CreateCompilationFromSources(
        string[] sources,
        bool includeAnnotationSource = true,
        IEnumerable<MetadataReference>? additionalReferences = null)
    {
        var syntaxTreeList = new List<SyntaxTree>();
        
        if (includeAnnotationSource)
        {
            syntaxTreeList.Add(CSharpSyntaxTree.ParseText(NPA_ANNOTATIONS_SOURCE));
        }
        
        foreach (var source in sources)
        {
            syntaxTreeList.Add(CSharpSyntaxTree.ParseText(source));
        }
        
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RepositoryAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Core.Repositories.IRepository<,>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
        };

        if (additionalReferences != null)
        {
            references.AddRange(additionalReferences);
        }

        return CSharpCompilation.Create(
            "TestAssembly",
            syntaxTreeList,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    #region Reflection Helpers for Testing Internal Generator Components

    /// <summary>
    /// Gets a type from the generator assembly by its full name.
    /// </summary>
    /// <param name="fullTypeName">The fully qualified type name (e.g., "NPA.Design.Models.RepositoryInfo").</param>
    /// <returns>The Type if found, otherwise null.</returns>
    protected static Type? GetGeneratorType(string fullTypeName)
    {
        var assembly = typeof(RepositoryGenerator).Assembly;
        return assembly.GetType(fullTypeName);
    }

    /// <summary>
    /// Gets a method from a type using reflection.
    /// </summary>
    /// <param name="typeName">The fully qualified type name.</param>
    /// <param name="methodName">The method name.</param>
    /// <param name="bindingFlags">The binding flags for the method lookup.</param>
    /// <returns>The MethodInfo if found, otherwise null.</returns>
    protected static MethodInfo? GetGeneratorMethod(string typeName, string methodName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static)
    {
        var type = GetGeneratorType(typeName);
        return type?.GetMethod(methodName, bindingFlags);
    }

    /// <summary>
    /// Sets a property value on an object using reflection.
    /// </summary>
    /// <param name="obj">The object to set the property on.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="value">The value to set.</param>
    protected static void SetPropertyValue(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        property?.SetValue(obj, value);
    }

    /// <summary>
    /// Gets a property value from an object using reflection.
    /// </summary>
    /// <param name="obj">The object to get the property from.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The property value, or null if not found.</returns>
    protected static object? GetPropertyValue(object obj, string propertyName)
    {
        var property = obj.GetType().GetProperty(propertyName);
        return property?.GetValue(obj);
    }

    /// <summary>
    /// Creates a RepositoryInfo instance with basic properties set.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <param name="keyType">The key type name.</param>
    /// <param name="interfaceName">Optional interface name.</param>
    /// <param name="namespace">Optional namespace.</param>
    /// <returns>A RepositoryInfo instance with properties initialized.</returns>
    protected static object CreateRepositoryInfo(
        string entityType,
        string keyType,
        string? interfaceName = null,
        string? @namespace = null)
    {
        var repositoryInfoType = GetGeneratorType("NPA.Design.Models.RepositoryInfo");
        repositoryInfoType.Should().NotBeNull("RepositoryInfo type should exist");
        var instance = Activator.CreateInstance(repositoryInfoType!)!;

        SetPropertyValue(instance, "EntityType", entityType);
        SetPropertyValue(instance, "KeyType", keyType);
        
        if (!string.IsNullOrEmpty(interfaceName))
        {
            SetPropertyValue(instance, "InterfaceName", interfaceName);
        }
        
        if (!string.IsNullOrEmpty(@namespace))
        {
            SetPropertyValue(instance, "Namespace", @namespace);
            if (!string.IsNullOrEmpty(interfaceName))
            {
                SetPropertyValue(instance, "FullInterfaceName", $"{@namespace}.{interfaceName}");
            }
        }

        // Initialize collections
        InitializeRepositoryInfoCollections(instance, repositoryInfoType!);

        return instance;
    }

    /// <summary>
    /// Initializes all collection properties on a RepositoryInfo instance.
    /// </summary>
    /// <param name="instance">The RepositoryInfo instance.</param>
    /// <param name="repositoryInfoType">The RepositoryInfo type.</param>
    private static void InitializeRepositoryInfoCollections(object instance, Type repositoryInfoType)
    {
        // Initialize Methods collection
        var methodsProperty = repositoryInfoType.GetProperty("Methods");
        if (methodsProperty != null)
        {
            var methodInfoType = repositoryInfoType.Assembly.GetType("NPA.Design.Models.MethodInfo");
            if (methodInfoType != null)
            {
                var listType = typeof(List<>).MakeGenericType(methodInfoType);
                var methods = Activator.CreateInstance(listType)!;
                methodsProperty.SetValue(instance, methods);
            }
        }

        // Initialize CompositeKeyProperties
        var compositeKeyPropsProperty = repositoryInfoType.GetProperty("CompositeKeyProperties");
        if (compositeKeyPropsProperty != null)
        {
            var compositeKeyProps = new List<string>();
            compositeKeyPropsProperty.SetValue(instance, compositeKeyProps);
        }

        // Initialize ManyToManyRelationships
        var manyToManyProperty = repositoryInfoType.GetProperty("ManyToManyRelationships");
        if (manyToManyProperty != null)
        {
            var manyToManyType = repositoryInfoType.Assembly.GetType("NPA.Design.Models.ManyToManyRelationshipInfo");
            if (manyToManyType != null)
            {
                var manyToManyListType = typeof(List<>).MakeGenericType(manyToManyType);
                var manyToMany = Activator.CreateInstance(manyToManyListType)!;
                manyToManyProperty.SetValue(instance, manyToMany);
            }
        }

        // Initialize Relationships collection
        var relationshipsProperty = repositoryInfoType.GetProperty("Relationships");
        if (relationshipsProperty != null)
        {
            var relationshipType = repositoryInfoType.Assembly.GetType("NPA.Design.Models.RelationshipMetadata");
            if (relationshipType != null)
            {
                var relationshipsListType = typeof(List<>).MakeGenericType(relationshipType);
                var relationships = Activator.CreateInstance(relationshipsListType)!;
                relationshipsProperty.SetValue(instance, relationships);
            }
        }
    }

    #endregion
}
