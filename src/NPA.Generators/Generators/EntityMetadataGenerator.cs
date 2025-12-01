using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using NPA.Generators.Shared;
using NPA.Generators.Models;

namespace NPA.Generators.Generators;

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

        if (classSymbol == null || !classSymbol.GetAttributes().Any(a => a.AttributeClass?.Name is "EntityAttribute" or "Entity"))
            return null;

        // Use shared metadata extractor for basic entity metadata (table, schema, properties)
        var basicInfo = MetadataExtractor.ExtractEntityMetadata(classSymbol);
        if (basicInfo == null) return null;
        
        // Use local complex relationship extraction (includes JoinColumn, JoinTable, MappedBy)
        var relationships = GetRelationships(classSymbol);
        
        // Return combined metadata
        return new EntityMetadataInfo
        {
            Name = basicInfo.Name,
            Namespace = basicInfo.Namespace,
            FullName = basicInfo.FullName, // Important: include FullName for code generation
            TableName = basicInfo.TableName,
            SchemaName = basicInfo.SchemaName,
            Properties = basicInfo.Properties,
            Relationships = relationships, // Use complex extraction instead
            NamedQueries = basicInfo.NamedQueries
        };
    }

    private static string? GetCollectionElementTypeSymbol(ITypeSymbol typeSymbol)
    {
        return (typeSymbol as INamedTypeSymbol)?.TypeArguments.FirstOrDefault()?.ToDisplayString();
    }
    
    private static string? ExtractMappedByFromSyntax(SyntaxNode propertySyntax, string attributeName)
    {
        // Find the attribute syntax with the matching name
        var attributeLists = propertySyntax.ChildNodes().OfType<AttributeListSyntax>();
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attrName = attribute.Name.ToString();
                if (attrName == attributeName || attrName == $"{attributeName}Attribute")
                {
                    // Look for MappedBy argument
                    if (attribute.ArgumentList != null)
                    {
                        foreach (var arg in attribute.ArgumentList.Arguments)
                        {
                            // Check if it's a named argument like "MappedBy = \"User\""
                            if (arg.NameEquals != null && arg.NameEquals.Name.Identifier.Text == "MappedBy")
                            {
                                // Extract the string literal value
                                if (arg.Expression is LiteralExpressionSyntax literal)
                                {
                                    return literal.Token.ValueText;
                                }
                            }
                            // Also check if it's a positional argument (first parameter)
                            else if (arg.NameEquals == null && attribute.ArgumentList.Arguments.IndexOf(arg) == 0)
                            {
                                if (arg.Expression is LiteralExpressionSyntax literal)
                                {
                                    return literal.Token.ValueText;
                                }
                            }
                        }
                    }
                }
            }
        }
        return null;
    }
    
    private static JoinColumnSyntaxValues? ExtractJoinColumnFromSyntax(SyntaxNode propertySyntax)
    {
        // Find the JoinColumn attribute syntax
        var attributeLists = propertySyntax.ChildNodes().OfType<AttributeListSyntax>();
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attrName = attribute.Name.ToString();
                if (attrName == "JoinColumn" || attrName == "JoinColumnAttribute")
                {
                    var result = new JoinColumnSyntaxValues();
                    
                    if (attribute.ArgumentList != null)
                    {
                        foreach (var arg in attribute.ArgumentList.Arguments)
                        {
                            if (arg.NameEquals != null)
                            {
                                var argName = arg.NameEquals.Name.Identifier.Text;
                                switch (argName)
                                {
                                    case "ReferencedColumnName":
                                        if (arg.Expression is LiteralExpressionSyntax refLiteral)
                                        {
                                            result.ReferencedColumnName = refLiteral.Token.ValueText;
                                        }
                                        break;
                                    case "Unique":
                                        if (arg.Expression is LiteralExpressionSyntax uniqueLiteral && 
                                            uniqueLiteral.Token.Value is bool uniqueValue)
                                        {
                                            result.Unique = uniqueValue;
                                        }
                                        break;
                                    case "Nullable":
                                        if (arg.Expression is LiteralExpressionSyntax nullableLiteral && 
                                            nullableLiteral.Token.Value is bool nullableValue)
                                        {
                                            result.Nullable = nullableValue;
                                        }
                                        break;
                                    case "Insertable":
                                        if (arg.Expression is LiteralExpressionSyntax insertableLiteral && 
                                            insertableLiteral.Token.Value is bool insertableValue)
                                        {
                                            result.Insertable = insertableValue;
                                        }
                                        break;
                                    case "Updatable":
                                        if (arg.Expression is LiteralExpressionSyntax updatableLiteral && 
                                            updatableLiteral.Token.Value is bool updatableValue)
                                        {
                                            result.Updatable = updatableValue;
                                        }
                                        break;
                                }
                            }
                            else if (attribute.ArgumentList.Arguments.IndexOf(arg) == 0)
                            {
                                // First positional argument is the column name
                                if (arg.Expression is LiteralExpressionSyntax literal)
                                {
                                    result.Name = literal.Token.ValueText;
                                }
                            }
                        }
                    }
                    
                    return result;
                }
            }
        }
        return null;
    }
    
    private class JoinColumnSyntaxValues
    {
        public string? Name { get; set; }
        public string? ReferencedColumnName { get; set; }
        public bool Unique { get; set; } = false;
        public bool Nullable { get; set; } = true;
        public bool Insertable { get; set; } = true;
        public bool Updatable { get; set; } = true;
    }
    
    private static JoinTableSyntaxValues? ExtractJoinTableFromSyntax(SyntaxNode propertySyntax)
    {
        // Find the JoinTable attribute syntax
        var attributeLists = propertySyntax.ChildNodes().OfType<AttributeListSyntax>();
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var attrName = attribute.Name.ToString();
                if (attrName == "JoinTable" || attrName == "JoinTableAttribute")
                {
                    var result = new JoinTableSyntaxValues();
                    
                    if (attribute.ArgumentList != null)
                    {
                        foreach (var arg in attribute.ArgumentList.Arguments)
                        {
                            if (arg.NameEquals != null)
                            {
                                var argName = arg.NameEquals.Name.Identifier.Text;
                                switch (argName)
                                {
                                    case "Schema":
                                        if (arg.Expression is LiteralExpressionSyntax schemaLiteral)
                                        {
                                            result.Schema = schemaLiteral.Token.ValueText;
                                        }
                                        break;
                                    case "JoinColumns":
                                        result.JoinColumns = ExtractStringArrayFromSyntax(arg.Expression);
                                        break;
                                    case "InverseJoinColumns":
                                        result.InverseJoinColumns = ExtractStringArrayFromSyntax(arg.Expression);
                                        break;
                                }
                            }
                            else if (attribute.ArgumentList.Arguments.IndexOf(arg) == 0)
                            {
                                // First positional argument is the table name
                                if (arg.Expression is LiteralExpressionSyntax literal)
                                {
                                    result.Name = literal.Token.ValueText;
                                }
                            }
                        }
                    }
                    
                    return result;
                }
            }
        }
        return null;
    }
    
    private static List<string>? ExtractStringArrayFromSyntax(ExpressionSyntax expression)
    {
        // Handle array creation: new[] { "value1", "value2" }
        if (expression is ArrayCreationExpressionSyntax arrayCreation)
        {
            if (arrayCreation.Initializer != null)
            {
                return arrayCreation.Initializer.Expressions
                    .OfType<LiteralExpressionSyntax>()
                    .Select(lit => lit.Token.ValueText)
                    .ToList();
            }
        }
        // Handle implicit array: new[] { ... } or { ... }
        else if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
        {
            return implicitArray.Initializer.Expressions
                .OfType<LiteralExpressionSyntax>()
                .Select(lit => lit.Token.ValueText)
                .ToList();
        }
        return null;
    }
    
    private class JoinTableSyntaxValues
    {
        public string? Name { get; set; }
        public string? Schema { get; set; }
        public List<string>? JoinColumns { get; set; }
        public List<string>? InverseJoinColumns { get; set; }
    }

    private static List<RelationshipMetadataInfo> GetRelationships(INamedTypeSymbol classSymbol)
    {
        var relationships = new List<RelationshipMetadataInfo>();

        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            // Get the syntax node for the property to access attribute syntax directly
            var propertySyntax = member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
            
            foreach (var attr in member.GetAttributes())
            {
                var attrName = attr.AttributeClass?.Name;
                string? targetEntity = null;
                string? relType = null;
                string? mappedBy = null;

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
                    
                    // Extract MappedBy from constructor arguments or named arguments
                    if (attr.ConstructorArguments.Length > 0)
                    {
                        mappedBy = attr.ConstructorArguments[0].Value?.ToString();
                    }
                    
                    // Check named arguments (these override constructor arguments)
                    foreach (var namedArg in attr.NamedArguments)
                    {
                        if (namedArg.Key == "MappedBy")
                        {
                            mappedBy = namedArg.Value.Value?.ToString();
                            break;
                        }
                    }
                    
                    // FALLBACK: If semantic model didn't give us the value, parse from syntax directly
                    if (string.IsNullOrEmpty(mappedBy) && propertySyntax != null)
                    {
                        mappedBy = ExtractMappedByFromSyntax(propertySyntax, "OneToOne");
                    }
                }
                else if (attrName == "OneToManyAttribute" || attrName == "OneToMany")
                {
                    relType = "OneToMany";
                    targetEntity = GetCollectionElementTypeSymbol(member.Type);
                    
                    // Extract MappedBy from constructor arguments or named arguments
                    if (attr.ConstructorArguments.Length > 0)
                    {
                        mappedBy = attr.ConstructorArguments[0].Value?.ToString();
                    }
                    
                    // Check named arguments (these override constructor arguments)
                    foreach (var namedArg in attr.NamedArguments)
                    {
                        if (namedArg.Key == "MappedBy")
                        {
                            mappedBy = namedArg.Value.Value?.ToString();
                            break;
                        }
                    }
                    
                    // FALLBACK: If semantic model didn't give us the value, parse from syntax directly
                    if (string.IsNullOrEmpty(mappedBy) && propertySyntax != null)
                    {
                        mappedBy = ExtractMappedByFromSyntax(propertySyntax, "OneToMany");
                    }
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
                    // Extract JoinColumn information
                    string? joinColumnName = null;
                    string? referencedColumnName = null;
                    bool isNullable = true;
                    bool isUnique = false;
                    bool isInsertable = true;
                    bool isUpdatable = true;
                    
                    var joinColumnAttr = member.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == "JoinColumnAttribute" || a.AttributeClass?.Name == "JoinColumn");
                    
                    if (joinColumnAttr != null)
                    {
                        // Get the column name from constructor argument or named argument
                        if (joinColumnAttr.ConstructorArguments.Length > 0)
                        {
                            joinColumnName = joinColumnAttr.ConstructorArguments[0].Value?.ToString();
                        }
                        
                        // Check for named arguments
                        foreach (var namedArg in joinColumnAttr.NamedArguments)
                        {
                            if (namedArg.Key == "ReferencedColumnName")
                            {
                                referencedColumnName = namedArg.Value.Value?.ToString();
                            }
                            else if (namedArg.Key == "Nullable")
                            {
                                if (namedArg.Value.Value is bool nullable)
                                {
                                    isNullable = nullable;
                                }
                            }
                            else if (namedArg.Key == "Unique")
                            {
                                if (namedArg.Value.Value is bool unique)
                                {
                                    isUnique = unique;
                                }
                            }
                            else if (namedArg.Key == "Insertable")
                            {
                                if (namedArg.Value.Value is bool insertable)
                                {
                                    isInsertable = insertable;
                                }
                            }
                            else if (namedArg.Key == "Updatable")
                            {
                                if (namedArg.Value.Value is bool updatable)
                                {
                                    isUpdatable = updatable;
                                }
                            }
                        }
                        
                        // FALLBACK: Extract from syntax if semantic model didn't give us values
                        if (propertySyntax != null && joinColumnAttr.NamedArguments.Length == 0)
                        {
                            var syntaxValues = ExtractJoinColumnFromSyntax(propertySyntax);
                            if (syntaxValues != null)
                            {
                                joinColumnName = syntaxValues.Name ?? joinColumnName;
                                referencedColumnName = syntaxValues.ReferencedColumnName ?? referencedColumnName;
                                isNullable = syntaxValues.Nullable;
                                isUnique = syntaxValues.Unique;
                                isInsertable = syntaxValues.Insertable;
                                isUpdatable = syntaxValues.Updatable;
                            }
                        }
                    }
                    
                    // Extract JoinTable information (for ManyToMany)
                    string? joinTableName = null;
                    string? joinTableSchema = null;
                    List<string>? joinColumns = null;
                    List<string>? inverseJoinColumns = null;
                    
                    var joinTableAttr = member.GetAttributes()
                        .FirstOrDefault(a => a.AttributeClass?.Name == "JoinTableAttribute" || a.AttributeClass?.Name == "JoinTable");
                    
                    if (joinTableAttr != null)
                    {
                        // Get the table name from constructor argument or named argument
                        if (joinTableAttr.ConstructorArguments.Length > 0)
                        {
                            joinTableName = joinTableAttr.ConstructorArguments[0].Value?.ToString();
                        }
                        
                        // Check for named arguments
                        foreach (var namedArg in joinTableAttr.NamedArguments)
                        {
                            if (namedArg.Key == "Schema")
                            {
                                joinTableSchema = namedArg.Value.Value?.ToString();
                            }
                            else if (namedArg.Key == "JoinColumns")
                            {
                                if (namedArg.Value.Kind == TypedConstantKind.Array)
                                {
                                    joinColumns = namedArg.Value.Values
                                        .Select(v => v.Value?.ToString())
                                        .Where(v => v != null)
                                        .Cast<string>()
                                        .ToList();
                                }
                            }
                            else if (namedArg.Key == "InverseJoinColumns")
                            {
                                if (namedArg.Value.Kind == TypedConstantKind.Array)
                                {
                                    inverseJoinColumns = namedArg.Value.Values
                                        .Select(v => v.Value?.ToString())
                                        .Where(v => v != null)
                                        .Cast<string>()
                                        .ToList();
                                }
                            }
                        }
                        
                        // FALLBACK: Extract from syntax if semantic model didn't give us values
                        if (propertySyntax != null && joinTableAttr.NamedArguments.Length == 0)
                        {
                            var syntaxValues = ExtractJoinTableFromSyntax(propertySyntax);
                            if (syntaxValues != null)
                            {
                                joinTableName = syntaxValues.Name ?? joinTableName;
                                joinTableSchema = syntaxValues.Schema ?? joinTableSchema;
                                joinColumns = syntaxValues.JoinColumns ?? joinColumns;
                                inverseJoinColumns = syntaxValues.InverseJoinColumns ?? inverseJoinColumns;
                            }
                        }
                    }
                    
                    relationships.Add(new RelationshipMetadataInfo
                    {
                        PropertyName = member.Name,
                        Type = relType,
                        TargetEntity = targetEntity,
                        JoinColumnName = joinColumnName,
                        ReferencedColumnName = referencedColumnName,
                        MappedBy = mappedBy,
                        IsNullable = isNullable,
                        IsUnique = isUnique,
                        IsInsertable = isInsertable,
                        IsUpdatable = isUpdatable,
                        IsOwner = relType == "ManyToOne" || (relType == "OneToOne" && string.IsNullOrEmpty(mappedBy)),
                        JoinTableName = joinTableName,
                        JoinTableSchema = joinTableSchema,
                        JoinColumns = joinColumns,
                        InverseJoinColumns = inverseJoinColumns
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
        sb.AppendLine("using System.Reflection;");
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
            sb.AppendLine($"                        PropertyInfo = typeof({entity.FullName}).GetProperty(\"{prop.Name}\")!,");
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
            sb.AppendLine($"                        TargetEntityType = typeof({rel.TargetEntity}),");
            
            // Add MappedBy if present
            sb.AppendLine($"                        MappedBy = {(string.IsNullOrEmpty(rel.MappedBy) ? "null" : $"\"{rel.MappedBy}\"")},");
            
            // Determine if we need to add JoinColumn or JoinTable
            bool hasJoinColumn = !string.IsNullOrEmpty(rel.JoinColumnName) || rel.Type == "ManyToOne" || (rel.Type == "OneToOne" && rel.IsOwner);
            bool hasJoinTable = rel.Type == "ManyToMany" && !string.IsNullOrEmpty(rel.JoinTableName);
            
            // Add IsOwner (with or without trailing comma based on whether JoinColumn or JoinTable follows)
            if (hasJoinColumn || hasJoinTable)
            {
                sb.AppendLine($"                        IsOwner = {(rel.IsOwner ? "true" : "false")},");
            }
            else
            {
                sb.AppendLine($"                        IsOwner = {(rel.IsOwner ? "true" : "false")}");
            }
            
            // Add JoinColumn metadata if present or required
            if (!string.IsNullOrEmpty(rel.JoinColumnName))
            {
                sb.AppendLine("                        JoinColumn = new JoinColumnMetadata");
                sb.AppendLine("                        {");
                sb.AppendLine($"                            Name = \"{rel.JoinColumnName}\",");
                sb.AppendLine($"                            ReferencedColumnName = \"{rel.ReferencedColumnName ?? "id"}\",");
                sb.AppendLine($"                            Unique = {(rel.IsUnique ? "true" : "false")},");
                sb.AppendLine($"                            Nullable = {(rel.IsNullable ? "true" : "false")},");
                sb.AppendLine($"                            Insertable = {(rel.IsInsertable ? "true" : "false")},");
                sb.AppendLine($"                            Updatable = {(rel.IsUpdatable ? "true" : "false")}");
                sb.AppendLine("                        }");
            }
            else if (rel.Type == "ManyToOne")
            {
                // For ManyToOne without explicit JoinColumn, generate default
                var defaultColumnName = ToSnakeCase(rel.PropertyName) + "_id";
                sb.AppendLine("                        JoinColumn = new JoinColumnMetadata");
                sb.AppendLine("                        {");
                sb.AppendLine($"                            Name = \"{defaultColumnName}\",");
                sb.AppendLine($"                            ReferencedColumnName = \"id\",");
                sb.AppendLine($"                            Unique = {(rel.IsUnique ? "true" : "false")},");
                sb.AppendLine($"                            Nullable = {(rel.IsNullable ? "true" : "false")},");
                sb.AppendLine($"                            Insertable = {(rel.IsInsertable ? "true" : "false")},");
                sb.AppendLine($"                            Updatable = {(rel.IsUpdatable ? "true" : "false")}");
                sb.AppendLine("                        }");
            }
            else if (rel.Type == "OneToOne" && rel.IsOwner)
            {
                // For OneToOne owner side without explicit JoinColumn, generate default
                var defaultColumnName = ToSnakeCase(rel.PropertyName) + "_id";
                sb.AppendLine("                        JoinColumn = new JoinColumnMetadata");
                sb.AppendLine("                        {");
                sb.AppendLine($"                            Name = \"{defaultColumnName}\",");
                sb.AppendLine($"                            ReferencedColumnName = \"id\",");
                sb.AppendLine($"                            Unique = {(rel.IsUnique ? "true" : "false")},");
                sb.AppendLine($"                            Nullable = {(rel.IsNullable ? "true" : "false")},");
                sb.AppendLine($"                            Insertable = {(rel.IsInsertable ? "true" : "false")},");
                sb.AppendLine($"                            Updatable = {(rel.IsUpdatable ? "true" : "false")}");
                sb.AppendLine("                        }");
            }
            
            // Add JoinTable metadata for ManyToMany relationships
            if (rel.Type == "ManyToMany" && !string.IsNullOrEmpty(rel.JoinTableName))
            {
                sb.AppendLine("                        JoinTable = new JoinTableMetadata");
                sb.AppendLine("                        {");
                
                var hasSchema = !string.IsNullOrEmpty(rel.JoinTableSchema);
                var hasJoinColumns = rel.JoinColumns != null && rel.JoinColumns.Any();
                var hasInverseJoinColumns = rel.InverseJoinColumns != null && rel.InverseJoinColumns.Any();
                
                // Name always comes first, add comma if any other properties follow
                sb.AppendLine($"                            Name = \"{rel.JoinTableName}\"{(hasSchema || hasJoinColumns || hasInverseJoinColumns ? "," : "")}");
                
                if (hasSchema)
                {
                    sb.AppendLine($"                            Schema = \"{rel.JoinTableSchema}\"{(hasJoinColumns || hasInverseJoinColumns ? "," : "")}");
                }
                if (hasJoinColumns)
                {
                    sb.AppendLine($"                            JoinColumns = new List<string> {{ {string.Join(", ", rel.JoinColumns.Select(c => $"\"{c}\""))} }}{(hasInverseJoinColumns ? "," : "")}");
                }
                if (hasInverseJoinColumns)
                {
                    sb.AppendLine($"                            InverseJoinColumns = new List<string> {{ {string.Join(", ", rel.InverseJoinColumns.Select(c => $"\"{c}\""))} }}");
                }
                sb.AppendLine("                        }");
            }
            
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
