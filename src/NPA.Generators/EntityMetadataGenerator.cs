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
    private static readonly string[] RelationshipAttributeNames = { "OneToOneAttribute", "OneToManyAttribute", "ManyToOneAttribute", "ManyToManyAttribute" };

    /// <summary>
    /// Initializes the incremental generator.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entityProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => IsEntityClass(node),
                transform: static (ctx, _) => GetEntityInfo(ctx))
            .Where(static info => info is not null);

        var allEntities = entityProvider.Collect();

        context.RegisterSourceOutput(allEntities, static (spc, entities) => GenerateMetadataProvider(spc, entities));
    }

    private static bool IsEntityClass(SyntaxNode node)
    {
        if (node is not ClassDeclarationSyntax classDecl)
            return false;
        return classDecl.AttributeLists.Count > 0;
    }

    private static EntityMetadataInfo? GetEntityInfo(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDecl) return null;
        var semanticModel = context.SemanticModel;
        var classSymbol = semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null || !classSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "EntityAttribute" || a.AttributeClass?.Name == "Entity"))
            return null;

        return new EntityMetadataInfo
        {
            Name = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            FullName = classSymbol.ToDisplayString(),
            TableName = GetTableName(classSymbol),
            SchemaName = GetSchemaName(classSymbol),
            Properties = GetProperties(classSymbol),
            Relationships = GetRelationships(classSymbol)
        };
    }

    private static string GetTableName(INamedTypeSymbol classSymbol)
    {
        var tableAttribute = classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");
        return tableAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? classSymbol.Name.ToLowerInvariant() + "s";
    }

    private static string? GetSchemaName(INamedTypeSymbol classSymbol)
    {
        var tableAttribute = classSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "TableAttribute" || a.AttributeClass?.Name == "Table");
        return tableAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "Schema").Value.Value?.ToString();
    }

    private static List<PropertyMetadataInfo> GetProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyMetadataInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.IsStatic || member.GetAttributes().Any(a => RelationshipAttributeNames.Contains(a.AttributeClass?.Name ?? "")))
                continue;

            // For typeof(), we need to strip nullable annotation from reference types
            var typeName = member.Type.ToDisplayString();
            if (member.Type.IsReferenceType && member.NullableAnnotation == NullableAnnotation.Annotated)
            {
                // Strip trailing '?' for nullable reference types (typeof doesn't support it)
                typeName = typeName.TrimEnd('?');
            }
            
            properties.Add(new PropertyMetadataInfo
            {
                Name = member.Name,
                TypeName = typeName,
                IsNullable = member.NullableAnnotation == NullableAnnotation.Annotated,
                IsPrimaryKey = HasAttribute(member, "IdAttribute", "Id"),
                IsIdentity = HasGeneratedValueAttribute(member),
                ColumnName = GetColumnName(member),
                IsRequired = HasAttribute(member, "RequiredAttribute", "Required") || !member.NullableAnnotation.Equals(NullableAnnotation.Annotated),
                IsUnique = GetIsUnique(member),
                Length = GetLength(member),
                Precision = GetPrecision(member),
                Scale = GetScale(member)
            });
        }

        return properties;
    }

    private static string GetColumnName(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");
        return columnAttribute?.ConstructorArguments.FirstOrDefault().Value?.ToString() ?? ToSnakeCase(property.Name);
    }

    private static bool HasAttribute(IPropertySymbol property, params string[] attributeNames)
    {
        return property.GetAttributes().Any(a => attributeNames.Contains(a.AttributeClass?.Name));
    }

    private static bool HasGeneratedValueAttribute(IPropertySymbol property)
    {
        return property.GetAttributes().Any(a => a.AttributeClass?.Name == "GeneratedValueAttribute" || a.AttributeClass?.Name == "GeneratedValue");
    }

    private static bool GetIsUnique(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");
        var uniqueArg = columnAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "IsUnique" || na.Key == "Unique");
        return uniqueArg?.Value.Value is bool isUnique && isUnique;
    }

    private static int? GetLength(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");
        var lengthArg = columnAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "Length");
        return lengthArg?.Value.Value as int?;
    }

    private static int? GetPrecision(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");
        var precisionArg = columnAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "Precision");
        return precisionArg?.Value.Value as int?;
    }

    private static int? GetScale(IPropertySymbol property)
    {
        var columnAttribute = property.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == "ColumnAttribute" || a.AttributeClass?.Name == "Column");
        var scaleArg = columnAttribute?.NamedArguments.FirstOrDefault(na => na.Key == "Scale");
        return scaleArg?.Value.Value as int?;
    }

    private static string? GetCollectionElementTypeSymbol(ITypeSymbol typeSymbol)
    {
        return (typeSymbol as INamedTypeSymbol)?.TypeArguments.FirstOrDefault()?.ToDisplayString();
    }

    private static List<RelationshipMetadataInfo> GetRelationships(INamedTypeSymbol classSymbol)
    {
        var relationships = new List<RelationshipMetadataInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            foreach (var attr in member.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                string? targetEntity = null;
                string? relType = null;

                if (attrName == "OneToOneAttribute" || attrName == "OneToOne")
                {
                    relType = "OneToOne";
                    var typeSymbol = member.Type;
                    // Strip nullable annotation for reference types
                    if (typeSymbol is INamedTypeSymbol namedType && namedType.IsReferenceType)
                    {
                        targetEntity = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                    }
                    else
                    {
                        targetEntity = typeSymbol.ToDisplayString().TrimEnd('?');
                    }
                }
                else if (attrName == "OneToManyAttribute" || attrName == "OneToMany")
                {
                    relType = "OneToMany";
                    targetEntity = GetCollectionElementTypeSymbol(member.Type);
                }
                else if (attrName == "ManyToOneAttribute" || attrName == "ManyToOne")
                {
                    relType = "ManyToOne";
                    var typeSymbol = member.Type;
                    // Strip nullable annotation for reference types
                    if (typeSymbol is INamedTypeSymbol namedType && namedType.IsReferenceType)
                    {
                        targetEntity = namedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
                    }
                    else
                    {
                        targetEntity = typeSymbol.ToDisplayString().TrimEnd('?');
                    }
                }
                else if (attrName == "ManyToManyAttribute" || attrName == "ManyToMany")
                {
                    relType = "ManyToMany";
                    targetEntity = GetCollectionElementTypeSymbol(member.Type);
                }

                if (relType != null && targetEntity != null)
                {
                    relationships.Add(new RelationshipMetadataInfo
                    {
                        PropertyName = member.Name,
                        Type = relType,
                        TargetEntity = targetEntity
                    });
                }
            }
        }

        return relationships;
    }

    private static void GenerateMetadataProvider(SourceProductionContext context, ImmutableArray<EntityMetadataInfo?> entities)
    {
        var validEntities = entities.Where(e => e != null).ToList();
        if (validEntities.Count == 0) return;

        var code = GenerateMetadataProviderCode(validEntities!);
        context.AddSource("GeneratedMetadataProvider.g.cs", SourceText.From(code, Encoding.UTF8));
    }

    private static string GenerateMetadataProviderCode(List<EntityMetadataInfo?> entities)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using NPA.Core.Metadata;");
        sb.AppendLine("using NPA.Core.Annotations;");
        sb.AppendLine();
        sb.AppendLine("namespace NPA.Generated");
        sb.AppendLine("{");
        sb.AppendLine("    public sealed class GeneratedMetadataProvider : IMetadataProvider");
        sb.AppendLine("    {");
        sb.AppendLine("        private static readonly Dictionary<Type, EntityMetadata> _metadata = new()");
        sb.AppendLine("        {");

        foreach (var entity in entities.Where(e => e != null))
        {
            sb.AppendLine($"            {{ typeof({entity!.FullName}), {entity.Name}Metadata() }},");
        }

        sb.AppendLine("        };");
        sb.AppendLine();
        sb.AppendLine("        public EntityMetadata GetEntityMetadata<T>() => GetEntityMetadata(typeof(T));");
        sb.AppendLine();
        sb.AppendLine("        public EntityMetadata GetEntityMetadata(Type entityType)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!_metadata.TryGetValue(entityType, out var metadata)) throw new ArgumentException($\"Entity type '{entityType.Name}' not found.\");");
        sb.AppendLine("            return metadata;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public EntityMetadata GetEntityMetadata(string entityName)");
        sb.AppendLine("        {");
        sb.AppendLine("            var entityType = _metadata.Keys.FirstOrDefault(t => t.Name == entityName);");
        sb.AppendLine("            if (entityType == null) throw new ArgumentException($\"Entity with name '{entityName}' not found.\");");
        sb.AppendLine("            return _metadata[entityType];");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        public bool IsEntity(Type type) => _metadata.ContainsKey(type);");
        sb.AppendLine();
        sb.AppendLine("        public IEnumerable<EntityMetadata> GetAllMetadata()");
        sb.AppendLine("        {");
        sb.AppendLine("            return _metadata.Values;");
        sb.AppendLine("        }");
        sb.AppendLine();

        foreach (var entity in entities.Where(e => e != null))
        {
            GenerateEntityMetadataFactory(sb, entity!);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateEntityMetadataFactory(StringBuilder sb, EntityMetadataInfo entity)
    {
        var primaryKeyProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);

        sb.AppendLine($"        private static EntityMetadata {entity.Name}Metadata()");
        sb.AppendLine("        {");
        sb.AppendLine("            return new EntityMetadata");
        sb.AppendLine("            {");
        sb.AppendLine($"                EntityType = typeof({entity.FullName}),");
        sb.AppendLine($"                TableName = \"{entity.TableName}\",");
        if (!string.IsNullOrEmpty(entity.SchemaName)) sb.AppendLine($"                SchemaName = \"{entity.SchemaName}\",");
        if (primaryKeyProp != null) sb.AppendLine($"                PrimaryKeyProperty = \"{primaryKeyProp.Name}\",");
        sb.AppendLine("                Properties = new Dictionary<string, PropertyMetadata>");
        sb.AppendLine("                {");

        for (int i = 0; i < entity.Properties.Count; i++)
        {
            var prop = entity.Properties[i];
            sb.AppendLine($"                    {{ \"{prop.Name}\", new PropertyMetadata");
            sb.AppendLine("                    {");
            sb.AppendLine($"                        PropertyName = \"{prop.Name}\",");
            sb.AppendLine($"                        ColumnName = \"{prop.ColumnName}\",");
            sb.AppendLine($"                        PropertyType = typeof({prop.TypeName}),");
            sb.AppendLine($"                        IsNullable = {prop.IsNullable.ToString().ToLower()},");
            sb.AppendLine($"                        IsPrimaryKey = {prop.IsPrimaryKey.ToString().ToLower()},");
            if (prop.IsIdentity) sb.AppendLine($"                        GenerationType = GenerationType.Identity,");
            sb.AppendLine($"                        IsUnique = {prop.IsUnique.ToString().ToLower()}");
            if (prop.Length.HasValue) sb.AppendLine($"                        , Length = {prop.Length.Value}");
            if (prop.Precision.HasValue) sb.AppendLine($"                        , Precision = {prop.Precision.Value}");
            if (prop.Scale.HasValue) sb.AppendLine($"                        , Scale = {prop.Scale.Value}");
            sb.Append("                    } }");
            if (i < entity.Properties.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.AppendLine("                },");
        sb.AppendLine("                Relationships = new Dictionary<string, RelationshipMetadata>");
        sb.AppendLine("                {");

        for (int j = 0; j < entity.Relationships.Count; j++)
        {
            var rel = entity.Relationships[j];
            sb.AppendLine($"                    {{ \"{rel.PropertyName}\", new RelationshipMetadata");
            sb.AppendLine("                    {");
            sb.AppendLine($"                        PropertyName = \"{rel.PropertyName}\",");
            sb.AppendLine($"                        RelationshipType = RelationshipType.{rel.Type},");
            sb.AppendLine($"                        TargetEntityType = typeof({rel.TargetEntity})");
            sb.Append("                    } }");
            if (j < entity.Relationships.Count - 1)
                sb.AppendLine(",");
            else
                sb.AppendLine();
        }

        sb.AppendLine("                }");
        sb.AppendLine("            };");
        sb.AppendLine("        }");
    }

    private static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
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
