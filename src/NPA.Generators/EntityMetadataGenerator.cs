using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace NPA.Generators;

/// <summary>
/// Incremental source generator that generates entity metadata at compile time to reduce runtime reflection overhead.
/// </summary>
[Generator]
public class EntityMetadataGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Create a provider that finds entity classes marked with [Entity] attribute
        var entityProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsEntityClass(node),
                transform: static (ctx, _) => GetEntityInfo(ctx))
            .Where(static info => info is not null);

        // Collect all entities
        var allEntities = entityProvider.Collect();

        // Register source output for generated metadata provider
        context.RegisterSourceOutput(allEntities, static (spc, entities) => GenerateMetadataProvider(spc, entities));
    }

    private static bool IsEntityClass(SyntaxNode node)
    {
        // Check if the node is a class with attributes
        if (node is not ClassDeclarationSyntax classDecl)
            return false;

        // Check if it has any attributes (we'll validate the specific attribute later)
        return classDecl.AttributeLists.Count > 0;
    }

    private static EntityMetadataInfo? GetEntityInfo(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null)
            return null;

        // Check if it has the Entity attribute
        var hasEntityAttribute = classSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.Name == "EntityAttribute" || a.AttributeClass?.Name == "Entity");

        if (!hasEntityAttribute)
            return null;

        // Extract entity information
        var entityInfo = new EntityMetadataInfo
        {
            Name = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            FullName = classSymbol.ToDisplayString(),
            TableName = GetTableName(classSymbol),
            SchemaName = GetSchemaName(classSymbol),
            Properties = GetProperties(classSymbol),
            Relationships = GetRelationships(classSymbol)
        };

        return entityInfo;
    }

    private static string GetTableName(INamedTypeSymbol classSymbol)
    {
        // Check for [Table] attribute
        var tableAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");

        if (tableAttribute != null && tableAttribute.ConstructorArguments.Length > 0)
        {
            return tableAttribute.ConstructorArguments[0].Value?.ToString() ?? classSymbol.Name;
        }

        // Default: use class name
        return classSymbol.Name.ToLowerInvariant() + "s";
    }

    private static string? GetSchemaName(INamedTypeSymbol classSymbol)
    {
        // Check for [Table] attribute with schema parameter
        var tableAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");

        if (tableAttribute != null)
        {
            // Look for Schema named parameter
            var schemaArg = tableAttribute.NamedArguments.FirstOrDefault(na => na.Key == "Schema");
            if (!schemaArg.Equals(default(KeyValuePair<string, TypedConstant>)))
            {
                return schemaArg.Value.Value?.ToString();
            }
        }

        return null;
    }

    private static List<PropertyMetadataInfo> GetProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyMetadataInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Skip static properties
            if (member.IsStatic)
                continue;

            var propertyInfo = new PropertyMetadataInfo
            {
                Name = member.Name,
                TypeName = member.Type.ToDisplayString(),
                IsNullable = member.NullableAnnotation == NullableAnnotation.Annotated,
                IsPrimaryKey = HasAttribute(member, "IdAttribute", "Id"),
                IsIdentity = HasGeneratedValueAttribute(member),
                ColumnName = GetColumnName(member),
                IsRequired = HasAttribute(member, "RequiredAttribute", "Required") || !member.NullableAnnotation.Equals(NullableAnnotation.Annotated),
                IsUnique = GetIsUnique(member),
                Length = GetLength(member),
                Precision = GetPrecision(member),
                Scale = GetScale(member)
            };

            properties.Add(propertyInfo);
        }

        return properties;
    }

    private static string GetColumnName(IPropertySymbol property)
    {
        // Check for [Column] attribute
        var columnAttribute = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        if (columnAttribute != null && columnAttribute.ConstructorArguments.Length > 0)
        {
            return columnAttribute.ConstructorArguments[0].Value?.ToString() ?? property.Name;
        }

        // Default: convert to snake_case
        return ToSnakeCase(property.Name);
    }

    private static bool HasAttribute(IPropertySymbol property, params string[] attributeNames)
    {
        return property.GetAttributes()
            .Any(a => attributeNames.Any(name => a.AttributeClass?.Name == name));
    }

    private static bool HasGeneratedValueAttribute(IPropertySymbol property)
    {
        var attr = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "GeneratedValueAttribute" || a.AttributeClass?.Name == "GeneratedValue");

        return attr != null;
    }

    private static bool GetIsUnique(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        if (columnAttribute != null)
        {
            var uniqueArg = columnAttribute.NamedArguments.FirstOrDefault(na => na.Key == "IsUnique" || na.Key == "Unique");
            if (!uniqueArg.Equals(default(KeyValuePair<string, TypedConstant>)))
            {
                return (bool)(uniqueArg.Value.Value ?? false);
            }
        }

        return false;
    }

    private static int? GetLength(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        if (columnAttribute != null)
        {
            var lengthArg = columnAttribute.NamedArguments.FirstOrDefault(na => na.Key == "Length");
            if (!lengthArg.Equals(default(KeyValuePair<string, TypedConstant>)))
            {
                return (int?)lengthArg.Value.Value;
            }
        }

        return null;
    }

    private static int? GetPrecision(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        if (columnAttribute != null)
        {
            var precisionArg = columnAttribute.NamedArguments.FirstOrDefault(na => na.Key == "Precision");
            if (!precisionArg.Equals(default(KeyValuePair<string, TypedConstant>)))
            {
                return (int?)precisionArg.Value.Value;
            }
        }

        return null;
    }

    private static int? GetScale(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");

        if (columnAttribute != null)
        {
            var scaleArg = columnAttribute.NamedArguments.FirstOrDefault(na => na.Key == "Scale");
            if (!scaleArg.Equals(default(KeyValuePair<string, TypedConstant>)))
            {
                return (int?)scaleArg.Value.Value;
            }
        }

        return null;
    }

    private static List<RelationshipMetadataInfo> GetRelationships(INamedTypeSymbol classSymbol)
    {
        var relationships = new List<RelationshipMetadataInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Check for relationship attributes
            foreach (var attr in member.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                if (attrName == "OneToManyAttribute" || attrName == "OneToMany")
                {
                    relationships.Add(new RelationshipMetadataInfo
                    {
                        PropertyName = member.Name,
                        Type = "OneToMany",
                        TargetEntity = member.Type.ToDisplayString()
                    });
                }
                else if (attrName == "ManyToOneAttribute" || attrName == "ManyToOne")
                {
                    relationships.Add(new RelationshipMetadataInfo
                    {
                        PropertyName = member.Name,
                        Type = "ManyToOne",
                        TargetEntity = member.Type.ToDisplayString()
                    });
                }
                else if (attrName == "ManyToManyAttribute" || attrName == "ManyToMany")
                {
                    relationships.Add(new RelationshipMetadataInfo
                    {
                        PropertyName = member.Name,
                        Type = "ManyToMany",
                        TargetEntity = member.Type.ToDisplayString()
                    });
                }
            }
        }

        return relationships;
    }

    private static void GenerateMetadataProvider(SourceProductionContext context, ImmutableArray<EntityMetadataInfo?> entities)
    {
        var validEntities = entities.Where(e => e != null).ToList();
        
        if (validEntities.Count == 0)
            return;

        var code = GenerateMetadataProviderCode(validEntities!);
        context.AddSource("GeneratedMetadataProvider.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static string GenerateMetadataProviderCode(List<EntityMetadataInfo?> entities)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("// This code was generated by NPA.Generators.EntityMetadataGenerator");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using NPA.Core.Metadata;");
        sb.AppendLine();
        sb.AppendLine("namespace NPA.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated metadata provider that provides pre-computed entity metadata to avoid runtime reflection.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static class GeneratedMetadataProvider");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly Dictionary<Type, EntityMetadata> _metadata = new()");
        sb.AppendLine("    {");

        foreach (var entity in entities.Where(e => e != null))
        {
            sb.AppendLine($"        {{ typeof({entity!.FullName}), {entity.Name}Metadata() }},");
        }

        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the metadata for the specified entity type.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static EntityMetadata? GetMetadata(Type entityType)");
        sb.AppendLine("    {");
        sb.AppendLine("        _metadata.TryGetValue(entityType, out var metadata);");
        sb.AppendLine("        return metadata;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all registered entity metadata.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static IEnumerable<EntityMetadata> GetAllMetadata()");
        sb.AppendLine("    {");
        sb.AppendLine("        return _metadata.Values;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate factory methods for each entity
        foreach (var entity in entities.Where(e => e != null))
        {
            GenerateEntityMetadataFactory(sb, entity!);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateEntityMetadataFactory(StringBuilder sb, EntityMetadataInfo entity)
    {
        var primaryKeyProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);

        sb.AppendLine($"    private static EntityMetadata {entity.Name}Metadata()");
        sb.AppendLine("    {");
        sb.AppendLine("        return new EntityMetadata");
        sb.AppendLine("        {");
        sb.AppendLine($"            EntityType = typeof({entity.FullName}),");
        sb.AppendLine($"            TableName = \"{entity.TableName}\",");
        
        if (!string.IsNullOrEmpty(entity.SchemaName))
        {
            sb.AppendLine($"            SchemaName = \"{entity.SchemaName}\",");
        }
        
        if (primaryKeyProp != null)
        {
            sb.AppendLine($"            PrimaryKeyProperty = \"{primaryKeyProp.Name}\",");
        }
        
        sb.AppendLine("            Properties = new Dictionary<string, PropertyMetadata>");
        sb.AppendLine("            {");

        foreach (var prop in entity.Properties)
        {
            sb.AppendLine($"                {{ \"{prop.Name}\", new PropertyMetadata");
            sb.AppendLine("                {");
            sb.AppendLine($"                    PropertyName = \"{prop.Name}\",");
            sb.AppendLine($"                    ColumnName = \"{prop.ColumnName}\",");
            sb.AppendLine($"                    PropertyType = typeof({prop.TypeName}),");
            sb.AppendLine($"                    IsNullable = {prop.IsNullable.ToString().ToLower()},");
            sb.AppendLine($"                    IsPrimaryKey = {prop.IsPrimaryKey.ToString().ToLower()},");
            if (prop.IsIdentity)
            {
                sb.AppendLine($"                    GenerationType = NPA.Core.Annotations.GenerationType.Identity,");
            }
            sb.AppendLine($"                    IsUnique = {prop.IsUnique.ToString().ToLower()},");
            
            if (prop.Length.HasValue)
            {
                sb.AppendLine($"                    Length = {prop.Length.Value},");
            }
            
            if (prop.Precision.HasValue)
            {
                sb.AppendLine($"                    Precision = {prop.Precision.Value},");
            }
            
            if (prop.Scale.HasValue)
            {
                sb.AppendLine($"                    Scale = {prop.Scale.Value},");
            }
            
            sb.AppendLine("                } },");
        }

        sb.AppendLine("            },");
        sb.AppendLine("            Relationships = new Dictionary<string, RelationshipMetadata>");
        sb.AppendLine("            {");

        foreach (var rel in entity.Relationships)
        {
            sb.AppendLine($"                {{ \"{rel.PropertyName}\", new RelationshipMetadata");
            sb.AppendLine("                {");
            sb.AppendLine($"                    PropertyName = \"{rel.PropertyName}\",");
            sb.AppendLine($"                    RelationshipType = RelationshipType.{rel.Type},");
            sb.AppendLine($"                    TargetEntityType = typeof({rel.TargetEntity}),");
            sb.AppendLine("                } },");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        };");
        sb.AppendLine("    }");
        sb.AppendLine();
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

internal class EntityMetadataInfo
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string? SchemaName { get; set; }
    public List<PropertyMetadataInfo> Properties { get; set; } = new();
    public List<RelationshipMetadataInfo> Relationships { get; set; } = new();
}

internal class PropertyMetadataInfo
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public int? Length { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
}

internal class RelationshipMetadataInfo
{
    public string PropertyName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
}

