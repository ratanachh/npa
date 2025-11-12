using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace NPA.Generators.Analyzers;

/// <summary>
/// Provides enhanced symbol information for generated repository methods.
/// This integrates with the Roslyn semantic model to provide IntelliSense-like information.
/// </summary>
public static class RepositorySymbolHelper
{
    /// <summary>
    /// Gets detailed information about a generated repository method for hover/IntelliSense display.
    /// </summary>
    public static string? GetMethodDocumentation(IMethodSymbol method, INamedTypeSymbol repositoryType)
    {
        if (method == null || repositoryType == null)
            return null;

        // Get the entity type from IRepository<T>
        var entityType = GetEntityType(repositoryType);
        if (entityType == null)
            return null;

        var methodName = method.Name;
        var entityName = entityType.Name;

        return methodName switch
        {
            "GetById" => $"Retrieves a {entityName} entity by its primary key.\n\nReturns: The entity if found, otherwise null.",
            "GetByIdAsync" => $"Asynchronously retrieves a {entityName} entity by its primary key.\n\nReturns: A task containing the entity if found, otherwise null.",
            "GetAll" => $"Retrieves all {entityName} entities from the repository.\n\nReturns: An enumerable collection of all entities.",
            "GetAllAsync" => $"Asynchronously retrieves all {entityName} entities from the repository.\n\nReturns: A task containing an enumerable collection of all entities.",
            "Add" => $"Adds a new {entityName} entity to the repository.\n\nNote: Call SaveChanges() to persist the changes to the database.",
            "AddAsync" => $"Asynchronously adds a new {entityName} entity to the repository.\n\nNote: Call SaveChangesAsync() to persist the changes to the database.",
            "Update" => $"Updates an existing {entityName} entity in the repository.\n\nNote: Call SaveChanges() to persist the changes to the database.",
            "UpdateAsync" => $"Asynchronously updates an existing {entityName} entity in the repository.\n\nNote: Call SaveChangesAsync() to persist the changes to the database.",
            "Delete" => $"Deletes a {entityName} entity by its primary key.\n\nNote: Call SaveChanges() to persist the changes to the database.",
            "DeleteAsync" => $"Asynchronously deletes a {entityName} entity by its primary key.\n\nNote: Call SaveChangesAsync() to persist the changes to the database.",
            "SaveChanges" => "Saves all pending changes to the database.\n\nReturns: The number of rows affected.",
            "SaveChangesAsync" => "Asynchronously saves all pending changes to the database.\n\nReturns: A task containing the number of rows affected.",
            _ => null
        };
    }

    /// <summary>
    /// Determines if a type symbol represents a generated repository.
    /// </summary>
    public static bool IsGeneratedRepository(ITypeSymbol typeSymbol)
    {
        return typeSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "GenerateRepositoryAttribute" ||
                        attr.AttributeClass?.Name == "GenerateRepository");
    }

    /// <summary>
    /// Gets the entity type from a repository type (from IRepository&lt;T&gt;).
    /// </summary>
    public static INamedTypeSymbol? GetEntityType(ITypeSymbol typeSymbol)
    {
        foreach (var iface in typeSymbol.AllInterfaces)
        {
            if (iface.Name == "IRepository" && iface.IsGenericType && iface.TypeArguments.Length == 1)
            {
                return iface.TypeArguments[0] as INamedTypeSymbol;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the primary key type for an entity.
    /// </summary>
    public static ITypeSymbol? GetPrimaryKeyType(INamedTypeSymbol entityType)
    {
        // Look for [PrimaryKey] attribute
        foreach (var member in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            var hasPrimaryKeyAttr = member.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "PrimaryKeyAttribute" ||
                            attr.AttributeClass?.Name == "PrimaryKey");

            if (hasPrimaryKeyAttr)
                return member.Type;
        }

        // Look for property named "Id"
        var idProperty = entityType.GetMembers("Id").OfType<IPropertySymbol>().FirstOrDefault();
        return idProperty?.Type;
    }

    /// <summary>
    /// Gets the primary key property for an entity.
    /// </summary>
    public static IPropertySymbol? GetPrimaryKeyProperty(INamedTypeSymbol entityType)
    {
        // Look for [PrimaryKey] attribute
        foreach (var member in entityType.GetMembers().OfType<IPropertySymbol>())
        {
            var hasPrimaryKeyAttr = member.GetAttributes()
                .Any(attr => attr.AttributeClass?.Name == "PrimaryKeyAttribute" ||
                            attr.AttributeClass?.Name == "PrimaryKey");

            if (hasPrimaryKeyAttr)
                return member;
        }

        // Look for property named "Id"
        return entityType.GetMembers("Id").OfType<IPropertySymbol>().FirstOrDefault();
    }
}
