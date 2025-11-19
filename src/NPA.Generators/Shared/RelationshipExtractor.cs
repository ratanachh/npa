using Microsoft.CodeAnalysis;
using NPA.Generators.Models;
using System.Collections.Generic;
using System.Linq;

namespace NPA.Generators.Shared;

/// <summary>
/// Extracts relationship metadata from entity properties for code generation.
/// </summary>
public static class RelationshipExtractor
{
    /// <summary>
    /// Extracts relationship metadata from a property symbol.
    /// </summary>
    public static RelationshipMetadata? ExtractRelationshipMetadata(IPropertySymbol propertySymbol)
    {
        var relationshipType = DetermineRelationshipType(propertySymbol);
        if (relationshipType == null)
            return null;

        var propertyType = propertySymbol.Type.ToDisplayString();
        var isCollection = IsCollectionType(propertySymbol.Type);
        
        string targetEntityType;
        string targetEntityFullType;
        
        if (isCollection)
        {
            // Extract type from ICollection<T>, List<T>, etc.
            var namedType = propertySymbol.Type as INamedTypeSymbol;
            if (namedType?.TypeArguments.Length > 0)
            {
                targetEntityFullType = namedType.TypeArguments[0].ToDisplayString();
                targetEntityType = namedType.TypeArguments[0].Name;
            }
            else
            {
                return null; // Can't determine collection element type
            }
        }
        else
        {
            targetEntityFullType = propertyType;
            targetEntityType = propertySymbol.Type.Name;
        }

        var relationship = new RelationshipMetadata
        {
            PropertyName = propertySymbol.Name,
            PropertyType = propertyType,
            Type = relationshipType.Value,
            TargetEntityType = targetEntityType,
            TargetEntityFullType = targetEntityFullType,
            IsCollection = isCollection,
            MappedBy = ExtractMappedByFromAttributes(propertySymbol),
            CascadeTypes = ExtractCascadeTypesFromAttributes(propertySymbol),
            FetchType = ExtractFetchTypeFromAttributes(propertySymbol),
            OrphanRemoval = HasOrphanRemovalFromAttributes(propertySymbol),
            Optional = ExtractOptionalFromAttributes(propertySymbol),
            JoinColumn = ExtractJoinColumnFromAttributes(propertySymbol),
            JoinTable = ExtractJoinTableFromAttributes(propertySymbol)
        };

        relationship.IsOwner = DetermineIfOwner(relationship);

        return relationship;
    }

    private static RelationshipType? DetermineRelationshipType(IPropertySymbol propertySymbol)
    {
        foreach (var attr in propertySymbol.GetAttributes())
        {
            var attrName = attr.AttributeClass?.Name;
            if (attrName == "OneToOneAttribute") return RelationshipType.OneToOne;
            if (attrName == "OneToManyAttribute") return RelationshipType.OneToMany;
            if (attrName == "ManyToOneAttribute") return RelationshipType.ManyToOne;
            if (attrName == "ManyToManyAttribute") return RelationshipType.ManyToMany;
        }
        return null;
    }

    private static bool IsCollectionType(ITypeSymbol typeSymbol)
    {
        var typeName = typeSymbol.ToDisplayString();
        return typeName.Contains("ICollection<") ||
               typeName.Contains("IList<") ||
               typeName.Contains("List<") ||
               typeName.Contains("IEnumerable<") ||
               typeName.Contains("HashSet<") ||
               typeName.Contains("ISet<");
    }

    private static string? ExtractMappedByFromAttributes(IPropertySymbol propertySymbol)
    {
        var attr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "OneToManyAttribute" || 
                               a.AttributeClass?.Name == "OneToOneAttribute" ||
                               a.AttributeClass?.Name == "ManyToManyAttribute");
        
        if (attr != null)
        {
            var mappedByArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == "MappedBy");
            if (mappedByArg.Value.Value is string mappedBy)
                return mappedBy;
        }
        return null;
    }

    private static int ExtractCascadeTypesFromAttributes(IPropertySymbol propertySymbol)
    {
        var attr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains("ToOne") == true ||
                               a.AttributeClass?.Name.Contains("ToMany") == true);
        
        if (attr != null)
        {
            var cascadeArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == "Cascade");
            if (cascadeArg.Value.Value is int cascadeValue)
                return cascadeValue;
        }
        return 0; // CascadeType.None
    }

    private static int ExtractFetchTypeFromAttributes(IPropertySymbol propertySymbol)
    {
        var attr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name.Contains("ToOne") == true ||
                               a.AttributeClass?.Name.Contains("ToMany") == true);
        
        if (attr != null)
        {
            var fetchArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == "Fetch");
            if (fetchArg.Value.Value is int fetchValue)
                return fetchValue;
        }
        return 1; // FetchType.Lazy (default)
    }

    private static bool HasOrphanRemovalFromAttributes(IPropertySymbol propertySymbol)
    {
        var attr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "OneToManyAttribute");
        
        if (attr != null)
        {
            var orphanArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == "OrphanRemoval");
            if (orphanArg.Value.Value is bool orphanValue)
                return orphanValue;
        }
        return false;
    }

    private static bool ExtractOptionalFromAttributes(IPropertySymbol propertySymbol)
    {
        var attr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ManyToOneAttribute" ||
                               a.AttributeClass?.Name == "OneToOneAttribute");
        
        if (attr != null)
        {
            var optionalArg = attr.NamedArguments.FirstOrDefault(arg => arg.Key == "Optional");
            if (optionalArg.Value.Value is bool optionalValue)
                return optionalValue;
        }
        return true; // Default is optional
    }

    private static JoinColumnInfo? ExtractJoinColumnFromAttributes(IPropertySymbol propertySymbol)
    {
        var attr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "JoinColumnAttribute");
        
        if (attr != null)
        {
            var joinColumn = new JoinColumnInfo();
            
            // Constructor argument (name)
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                joinColumn.Name = name;
            
            // Named arguments
            foreach (var namedArg in attr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Name" when namedArg.Value.Value is string n:
                        joinColumn.Name = n;
                        break;
                    case "ReferencedColumnName" when namedArg.Value.Value is string r:
                        joinColumn.ReferencedColumnName = r;
                        break;
                    case "Nullable" when namedArg.Value.Value is bool nullable:
                        joinColumn.Nullable = nullable;
                        break;
                    case "Unique" when namedArg.Value.Value is bool unique:
                        joinColumn.Unique = unique;
                        break;
                    case "Insertable" when namedArg.Value.Value is bool insertable:
                        joinColumn.Insertable = insertable;
                        break;
                    case "Updatable" when namedArg.Value.Value is bool updatable:
                        joinColumn.Updatable = updatable;
                        break;
                }
            }
            
            return joinColumn;
        }
        return null;
    }

    private static JoinTableInfo? ExtractJoinTableFromAttributes(IPropertySymbol propertySymbol)
    {
        var attr = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "JoinTableAttribute");
        
        if (attr != null)
        {
            var joinTable = new JoinTableInfo();
            
            // Constructor argument (name)
            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                joinTable.Name = name;
            
            // Named arguments
            foreach (var namedArg in attr.NamedArguments)
            {
                switch (namedArg.Key)
                {
                    case "Name" when namedArg.Value.Value is string n:
                        joinTable.Name = n;
                        break;
                    case "Schema" when namedArg.Value.Value is string s:
                        joinTable.Schema = s;
                        break;
                    case "JoinColumns" when namedArg.Value.Values.Length > 0:
                        joinTable.JoinColumns = namedArg.Value.Values.Select(v => v.Value?.ToString() ?? "").ToArray();
                        break;
                    case "InverseJoinColumns" when namedArg.Value.Values.Length > 0:
                        joinTable.InverseJoinColumns = namedArg.Value.Values.Select(v => v.Value?.ToString() ?? "").ToArray();
                        break;
                }
            }
            
            return joinTable;
        }
        return null;
    }

    private static bool DetermineIfOwner(RelationshipMetadata relationship)
    {
        // If it has MappedBy, it's the inverse side (not owner)
        if (!string.IsNullOrEmpty(relationship.MappedBy))
            return false;

        // ManyToOne is always the owner
        if (relationship.Type == RelationshipType.ManyToOne)
            return true;

        // OneToOne without MappedBy is the owner
        if (relationship.Type == RelationshipType.OneToOne)
            return true;

        // OneToMany without MappedBy is the owner (though unusual)
        if (relationship.Type == RelationshipType.OneToMany)
            return true;

        // ManyToMany ownership determined by MappedBy
        return relationship.Type == RelationshipType.ManyToMany;
    }
}
