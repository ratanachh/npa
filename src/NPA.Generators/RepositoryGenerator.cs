using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NPA.Generators;

/// <summary>
/// Source generator for creating repository implementations from interfaces marked with [Repository] attribute.
/// </summary>
[Generator]
public class RepositoryGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register a syntax provider that finds interfaces with Repository attribute
        var repositoryInterfaces = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsRepositoryInterface(node),
                transform: static (ctx, _) => GetRepositoryInfo(ctx))
            .Where(static info => info is not null);

        // Register the source output
        context.RegisterSourceOutput(repositoryInterfaces, static (spc, source) => GenerateRepository(spc, source!));
    }

    private static bool IsRepositoryInterface(SyntaxNode node)
    {
        // Check if the node is an interface with attributes
        if (node is not InterfaceDeclarationSyntax interfaceDecl)
            return false;

        // Check if it has any attributes (we'll validate the specific attribute later)
        return interfaceDecl.AttributeLists.Count > 0;
    }

    private static RepositoryInfo? GetRepositoryInfo(GeneratorSyntaxContext context)
    {
        var interfaceDecl = (InterfaceDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDecl) as INamedTypeSymbol;

        if (interfaceSymbol == null)
            return null;

        // Check if it has the Repository attribute
        var hasRepositoryAttribute = interfaceSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "RepositoryAttribute");

        if (!hasRepositoryAttribute)
            return null;

        // Extract entity and key types from IRepository<TEntity, TKey> inheritance
        var (entityType, keyType) = ExtractRepositoryTypes(interfaceSymbol);
        if (entityType == null || keyType == null)
            return null;

        var methods = interfaceSymbol.GetMembers()
            .OfType<IMethodSymbol>()
            .Select(m => new MethodInfo
            {
                Name = m.Name,
                ReturnType = m.ReturnType.ToDisplayString(),
                Parameters = m.Parameters.Select(p => new ParameterInfo
                {
                    Name = p.Name,
                    Type = p.Type.ToDisplayString()
                }).ToList()
            })
            .ToList();

        return new RepositoryInfo
        {
            InterfaceName = interfaceSymbol.Name,
            FullInterfaceName = interfaceSymbol.ToDisplayString(),
            Namespace = interfaceSymbol.ContainingNamespace.ToDisplayString(),
            EntityType = entityType,
            KeyType = keyType,
            Methods = methods
        };
    }

    private static (string? entityType, string? keyType) ExtractRepositoryTypes(INamedTypeSymbol interfaceSymbol)
    {
        // Look for IRepository<TEntity, TKey> in the interface hierarchy
        foreach (var baseInterface in interfaceSymbol.AllInterfaces)
        {
            if (baseInterface.Name == "IRepository" && baseInterface.TypeArguments.Length >= 2)
            {
                var entityType = baseInterface.TypeArguments[0].ToDisplayString();
                var keyType = baseInterface.TypeArguments[1].ToDisplayString();
                return (entityType, keyType);
            }
        }

        // Try to extract from attribute parameter (fallback)
        var repositoryAttr = interfaceSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RepositoryAttribute");

        if (repositoryAttr != null && repositoryAttr.ConstructorArguments.Length > 0)
        {
            var entityTypeArg = repositoryAttr.ConstructorArguments[0];
            if (entityTypeArg.Value is INamedTypeSymbol entityTypeSymbol)
            {
                return (entityTypeSymbol.ToDisplayString(), "object"); // Default key type
            }
        }

        // Default: try to infer from interface name (IUserRepository -> User)
        var interfaceName = interfaceSymbol.Name;
        if (interfaceName.StartsWith("I") && interfaceName.EndsWith("Repository"))
        {
            var entityType = interfaceName.Substring(1, interfaceName.Length - 11); // Remove I prefix and Repository suffix
            return (entityType, "object"); // Default key type
        }

        return (null, null);
    }

    private static void GenerateRepository(SourceProductionContext context, RepositoryInfo info)
    {
        var code = GenerateRepositoryCode(info);
        var repositoryName = info.InterfaceName;
        if (repositoryName.StartsWith("I"))
        {
            repositoryName = repositoryName.Substring(1);
        }
        context.AddSource($"{repositoryName}Implementation.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static string GenerateRepositoryCode(RepositoryInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This code was generated by NPA.Generators.RepositoryGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine("using Dapper;");
        sb.AppendLine("using NPA.Core.Core;");
        sb.AppendLine("using NPA.Core.Repositories;");
        sb.AppendLine("using NPA.Core.Metadata;");
        sb.AppendLine();

        sb.AppendLine($"namespace {info.Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Generated implementation of {info.InterfaceName}.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public partial class {GetImplementationName(info.InterfaceName)} : BaseRepository<{info.EntityType}, {info.KeyType}>, {info.FullInterfaceName}");
        sb.AppendLine("    {");
        sb.AppendLine($"        /// <summary>");
        sb.AppendLine($"        /// Initializes a new instance of the {GetImplementationName(info.InterfaceName)} class.");
        sb.AppendLine($"        /// </summary>");
        sb.AppendLine("        public " + GetImplementationName(info.InterfaceName) + "(IDbConnection connection, IEntityManager entityManager, IMetadataProvider metadataProvider)");
        sb.AppendLine("            : base(connection, entityManager, metadataProvider)");
        sb.AppendLine("        {");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Generate method implementations
        foreach (var method in info.Methods)
        {
            sb.AppendLine(GenerateMethodImplementation(method, info.EntityType, info.KeyType));
            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GetImplementationName(string interfaceName)
    {
        // IUserRepository -> UserRepository
        if (interfaceName.StartsWith("I") && char.IsUpper(interfaceName[1]))
            return interfaceName.Substring(1);
        
        return interfaceName + "Implementation";
    }

    private static string GenerateMethodImplementation(MethodInfo method, string entityType, string keyType)
    {
        var sb = new StringBuilder();

        // Add XML documentation
        sb.AppendLine("        /// <inheritdoc />");

        // Method signature - add async if return type is Task
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"));
        var isAsync = method.ReturnType.StartsWith("System.Threading.Tasks.Task");
        var asyncModifier = isAsync ? "async " : "";
        
        sb.AppendLine($"        public {asyncModifier}{method.ReturnType} {method.Name}({parameters})");
        sb.AppendLine("        {");

        // Analyze method name for conventions
        var implementation = GenerateMethodBody(method, entityType);
        sb.Append(implementation);

        sb.AppendLine("        }");

        return sb.ToString();
    }

    private static string GenerateMethodBody(MethodInfo method, string entityType)
    {
        var sb = new StringBuilder();

        // Simple convention analysis
        if (method.Name.StartsWith("GetAll") || method.Name.StartsWith("FindAll"))
        {
            sb.AppendLine($"            var sql = \"SELECT * FROM {GetTableName(entityType)}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{entityType}>(sql);");
        }
        else if (method.Name.StartsWith("GetById") || method.Name.StartsWith("FindById"))
        {
            sb.AppendLine($"            var sql = \"SELECT * FROM {GetTableName(entityType)} WHERE id = @id\";");
            sb.AppendLine($"            return await _connection.QueryFirstOrDefaultAsync<{entityType}>(sql, new {{ id }});");
        }
        else if (method.Name.StartsWith("FindBy") && method.Parameters.Count == 1)
        {
            var propertyName = method.Name.Substring(6).Replace("Async", "");
            var paramName = method.Parameters[0].Name;
            sb.AppendLine($"            var sql = \"SELECT * FROM {GetTableName(entityType)} WHERE {ToSnakeCase(propertyName)} = @{paramName}\";");
            sb.AppendLine($"            return await _connection.QueryAsync<{entityType}>(sql, new {{ {paramName} }});");
        }
        else if (method.Name.StartsWith("Save") || method.Name.StartsWith("Insert") || method.Name.StartsWith("Add"))
        {
            sb.AppendLine($"            await _entityManager.PersistAsync(entity);");
        }
        else if (method.Name.StartsWith("Update"))
        {
            sb.AppendLine($"            await _entityManager.MergeAsync(entity);");
        }
        else if (method.Name.StartsWith("Delete") || method.Name.StartsWith("Remove"))
        {
            if (method.Parameters.Any(p => p.Type.Contains(entityType)))
                sb.AppendLine($"            await _entityManager.RemoveAsync(entity);");
            else
                sb.AppendLine($"            await _entityManager.RemoveAsync<{entityType}>(id);");
        }
        else if (method.Name.StartsWith("Count"))
        {
            sb.AppendLine($"            var sql = \"SELECT COUNT(*) FROM {GetTableName(entityType)}\";");
            sb.AppendLine($"            return await _connection.ExecuteScalarAsync<int>(sql);");
        }
        else
        {
            // Default implementation - throw not implemented
            sb.AppendLine($"            throw new NotImplementedException(\"Method {method.Name} requires manual implementation\");");
        }

        return sb.ToString();
    }

    private static string GetTableName(string entityType)
    {
        // Simple pluralization: User -> users
        var simpleName = entityType.Split('.').Last();
        return ToSnakeCase(simpleName) + "s";
    }

    private static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var sb = new StringBuilder();
        sb.Append(char.ToLower(text[0]));

        for (int i = 1; i < text.Length; i++)
        {
            if (char.IsUpper(text[i]))
            {
                sb.Append('_');
                sb.Append(char.ToLower(text[i]));
            }
            else
            {
                sb.Append(text[i]);
            }
        }

        return sb.ToString();
    }
}

internal class RepositoryInfo
{
    public string InterfaceName { get; set; } = string.Empty;
    public string FullInterfaceName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string KeyType { get; set; } = string.Empty;
    public List<MethodInfo> Methods { get; set; } = new();
}

internal class MethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public List<ParameterInfo> Parameters { get; set; } = new();
}

internal class ParameterInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
