using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NPA.Generators.Shared;

namespace NPA.Generators;

/// <summary>
/// Generator for bidirectional relationship synchronization helper methods.
/// Generates static helper classes that maintain consistency between both sides of relationships.
/// </summary>
[Generator]
public class BidirectionalRelationshipGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all entity classes with [Entity] attribute
        var entityProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                transform: static (ctx, _) => GetEntityIfHasBidirectionalRelationships(ctx))
            .Where(static m => m is not null);

        // Combine and generate
        context.RegisterSourceOutput(entityProvider.Collect(), (spc, entities) =>
        {
            var entitiesWithRelationships = entities.Where(e => e != null).ToList();
            if (entitiesWithRelationships.Count == 0)
                return;

            // Build a lookup of entity relationships for cross-entity analysis
            var entityLookup = new Dictionary<string, EntityRelationshipInfo>();
            foreach (var entity in entitiesWithRelationships)
            {
                if (entity != null)
                {
                    var fullName = $"{entity.Namespace}.{entity.EntityName}";
                    entityLookup[fullName] = entity;
                }
            }

            // Resolve inverse property names for owner-side relationships
            foreach (var entity in entitiesWithRelationships)
            {
                if (entity == null) continue;

                foreach (var rel in entity.BidirectionalRelationships)
                {
                    if (rel.IsOwnerSide && entityLookup.TryGetValue(rel.TargetEntity, out var targetEntity))
                    {
                        // Find the inverse property that references back to us
                        var inverseRel = targetEntity.BidirectionalRelationships
                            .FirstOrDefault(r => r.MappedBy == rel.PropertyName && r.IsInverseSide);

                        if (inverseRel != null)
                        {
                            rel.InversePropertyName = inverseRel.PropertyName;
                        }
                    }
                }
            }

            // Generate helper classes for each entity with bidirectional relationships
            foreach (var entity in entitiesWithRelationships)
            {
                if (entity == null) continue;

                var source = GenerateHelperClass(entity);
                spc.AddSource($"{entity.EntityName}RelationshipHelper.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        });
    }

    private static EntityRelationshipInfo? GetEntityIfHasBidirectionalRelationships(GeneratorSyntaxContext context)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var model = context.SemanticModel;
        var classSymbol = model.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;

        if (classSymbol == null)
            return null;

        // Skip nested classes - they're edge cases that need special handling
        if (classSymbol.ContainingType != null)
            return null;

        // Check if it has [Entity] attribute
        if (!classSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "EntityAttribute" || a.AttributeClass?.Name == "Entity"))
            return null;

        // Extract all relationships
        var relationships = MetadataExtractor.ExtractRelationships(classSymbol);

        // Find bidirectional relationships (those with mappedBy or those that are referenced by mappedBy)
        var bidirectionalRelationships = new List<BidirectionalRelationship>();

        foreach (var rel in relationships)
        {
            // Check if this is the inverse side (has mappedBy)
            if (!string.IsNullOrEmpty(rel.MappedBy))
            {
                var ownerSidePropertyIsNullable = GetOwnerSidePropertyNullability(model, rel.TargetEntity, rel.MappedBy!);
                // Check if the target entity (owner side) has the FK property
                var targetEntityType = model.Compilation.GetTypeByMetadataName(rel.TargetEntity);
                var fkPropertyName = $"{rel.MappedBy}Id";
                var hasFk = targetEntityType?.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Any(p => p.Name == fkPropertyName) ?? false;

                bidirectionalRelationships.Add(new BidirectionalRelationship
                {
                    PropertyName = rel.PropertyName,
                    PropertyType = rel.Type == "OneToMany" || rel.Type == "ManyToMany" ? $"ICollection<{rel.TargetEntity}>" : rel.TargetEntity,
                    TargetEntity = rel.TargetEntity,
                    RelationshipType = rel.Type,
                    MappedBy = rel.MappedBy,
                    IsInverseSide = true,
                    IsOwnerSide = false,
                    IsCollection = rel.Type == "OneToMany" || rel.Type == "ManyToMany",
                    IsNullable = ownerSidePropertyIsNullable, // Store owner-side property nullability
                    HasForeignKeyProperty = hasFk // Store whether target entity has FK property
                });
            }
            // Check if this is the owner side (ManyToOne or OneToOne without mappedBy)
            else if (rel.Type == "ManyToOne" || rel.Type == "OneToOne")
            {
                var isPropertyNullable = GetPropertyNullability(classSymbol, rel.PropertyName);
                var inverseCollectionProperty = FindInverseCollectionProperty(model, rel.TargetEntity, rel.PropertyName);
                var fkPropertyName = $"{rel.PropertyName}Id";
                var hasFk = classSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Any(p => p.Name == fkPropertyName);

                bidirectionalRelationships.Add(new BidirectionalRelationship
                {
                    PropertyName = rel.PropertyName,
                    PropertyType = rel.TargetEntity,
                    TargetEntity = rel.TargetEntity,
                    RelationshipType = rel.Type,
                    MappedBy = null,
                    IsInverseSide = false,
                    IsOwnerSide = true,
                    IsCollection = false,
                    IsNullable = isPropertyNullable,
                    InverseCollectionProperty = inverseCollectionProperty,
                    HasForeignKeyProperty = hasFk
                });
            }
        }

        if (bidirectionalRelationships.Count == 0)
            return null;

        // Extract foreign key properties
        var fkProperties = new Dictionary<string, string>();
        foreach (var member in classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (member.Type.SpecialType == SpecialType.System_Int32 && member.Name.EndsWith("Id"))
            {
                fkProperties[member.Name] = member.Type.ToString();
            }
        }

        return new EntityRelationshipInfo
        {
            EntityName = classSymbol.Name,
            Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
            BidirectionalRelationships = bidirectionalRelationships,
            ForeignKeyProperties = fkProperties
        };
    }

    private static string GenerateHelperClass(EntityRelationshipInfo entity)
    {
        var sb = new StringBuilder();

        sb.AppendLine("#nullable enable");
        sb.AppendLine($"namespace {entity.Namespace};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Helper class for managing bidirectional relationships on {entity.EntityName}.");
        sb.AppendLine("/// Ensures both sides of relationships are automatically synchronized.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public static class {entity.EntityName}RelationshipHelper");
        sb.AppendLine("{");

        foreach (var rel in entity.BidirectionalRelationships)
        {
            if (rel.IsOwnerSide)
            {
                // Generate Set method for owner side (e.g., SetCustomer for Order.Customer)
                GenerateOwnerSideSetMethod(sb, entity, rel);
            }
            else if (rel.IsInverseSide && rel.IsCollection)
            {
                // Generate AddTo/RemoveFrom methods for collection inverse side (e.g., AddToOrders for Customer.Orders)
                GenerateInverseSideAddMethod(sb, entity, rel);
                GenerateInverseSideRemoveMethod(sb, entity, rel);
            }
        }

        // Generate validation methods for all bidirectional relationships
        GenerateValidationMethods(sb, entity);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateOwnerSideSetMethod(StringBuilder sb, EntityRelationshipInfo entity, BidirectionalRelationship rel)
    {
        var fkProperty = $"{rel.PropertyName}Id";
        var hasFk = entity.ForeignKeyProperties.ContainsKey(fkProperty);

        // Use fully qualified names for parameters
        var entityFullName = $"{entity.Namespace}.{entity.EntityName}";
        var targetFullName = rel.TargetEntity; // Already fully qualified from extraction

        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Sets the {rel.PropertyName} relationship and synchronizes the inverse side.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static void Set{rel.PropertyName}({entityFullName} entity, {targetFullName}? value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (entity == null) throw new System.ArgumentNullException(nameof(entity));");
        sb.AppendLine();
        sb.AppendLine($"        var oldValue = entity.{rel.PropertyName};");
        sb.AppendLine("        if (oldValue == value) return; // No change");
        sb.AppendLine();
        sb.AppendLine("        // Remove from old parent's collection");
        sb.AppendLine("        if (oldValue != null)");
        sb.AppendLine("        {");

        // Remove from old parent's collection
        if (!string.IsNullOrEmpty(rel.InverseCollectionProperty))
        {
            sb.AppendLine($"            if (oldValue.{rel.InverseCollectionProperty}?.Contains(entity) == true)");
            sb.AppendLine("            {");
            sb.AppendLine($"                oldValue.{rel.InverseCollectionProperty}.Remove(entity);");
            sb.AppendLine("            }");
        }
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Set new value");
        // Use null-forgiving operator if property is non-nullable but value parameter is nullable
        if (!rel.IsNullable)
        {
            sb.AppendLine($"        entity.{rel.PropertyName} = value!;");
        }
        else
        {
            sb.AppendLine($"        entity.{rel.PropertyName} = value;");
        }

        if (hasFk)
        {
            sb.AppendLine($"        entity.{fkProperty} = value?.Id ?? 0;");
        }

        sb.AppendLine();
        sb.AppendLine("        // Add to new parent's collection");
        if (!string.IsNullOrEmpty(rel.InverseCollectionProperty))
        {
            sb.AppendLine("        if (value != null)");
            sb.AppendLine("        {");
            sb.AppendLine($"            value.{rel.InverseCollectionProperty} ??= new System.Collections.Generic.List<{entityFullName}>();");
            sb.AppendLine($"            if (!value.{rel.InverseCollectionProperty}.Contains(entity))");
            sb.AppendLine("            {");
            sb.AppendLine($"                value.{rel.InverseCollectionProperty}.Add(entity);");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
        }
        sb.AppendLine("    }");
    }

    private static void GenerateInverseSideAddMethod(StringBuilder sb, EntityRelationshipInfo entity, BidirectionalRelationship rel)
    {
        var targetEntityName = rel.TargetEntity.Split('.').Last();
        var mappedByProperty = rel.MappedBy ?? entity.EntityName;

        // Use fully qualified names for parameters
        var entityFullName = $"{entity.Namespace}.{entity.EntityName}";
        var targetFullName = rel.TargetEntity;

        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Adds an item to {rel.PropertyName} collection and synchronizes the inverse side.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static void AddTo{rel.PropertyName}({entityFullName} entity, {targetFullName} item)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (entity == null) throw new System.ArgumentNullException(nameof(entity));");
        sb.AppendLine("        if (item == null) throw new System.ArgumentNullException(nameof(item));");
        sb.AppendLine();
        sb.AppendLine($"        entity.{rel.PropertyName} ??= new System.Collections.Generic.List<{rel.TargetEntity}>();");
        sb.AppendLine();
        sb.AppendLine("        // Add to collection if not already present");
        sb.AppendLine($"        if (!entity.{rel.PropertyName}.Contains(item))");
        sb.AppendLine("        {");
        sb.AppendLine($"            entity.{rel.PropertyName}.Add(item);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Set inverse side");
        sb.AppendLine($"        if (item.{mappedByProperty} != entity)");
        sb.AppendLine("        {");
        sb.AppendLine($"            item.{mappedByProperty} = entity;");
        sb.AppendLine();
        // Only set FK if the target entity has the FK property
        if (rel.HasForeignKeyProperty)
        {
            sb.AppendLine("            // Also set FK if exists");
            var fkPropertyName = $"{mappedByProperty}Id";
            sb.AppendLine($"            item.{fkPropertyName} = entity.Id;");
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateInverseSideRemoveMethod(StringBuilder sb, EntityRelationshipInfo entity, BidirectionalRelationship rel)
    {
        var targetEntityName = rel.TargetEntity.Split('.').Last();
        var mappedByProperty = rel.MappedBy ?? entity.EntityName;

        // Use fully qualified names for parameters
        var entityFullName = $"{entity.Namespace}.{entity.EntityName}";
        var targetFullName = rel.TargetEntity;

        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Removes an item from {rel.PropertyName} collection and synchronizes the inverse side.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static void RemoveFrom{rel.PropertyName}({entityFullName} entity, {targetFullName} item)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (entity == null) throw new System.ArgumentNullException(nameof(entity));");
        sb.AppendLine("        if (item == null) throw new System.ArgumentNullException(nameof(item));");
        sb.AppendLine();
        sb.AppendLine($"        if (entity.{rel.PropertyName}?.Contains(item) == true)");
        sb.AppendLine("        {");
        sb.AppendLine($"            entity.{rel.PropertyName}.Remove(item);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Clear inverse side");
        sb.AppendLine($"        if (item.{mappedByProperty} == entity)");
        sb.AppendLine("        {");
        // Only assign null if the owner-side property is nullable
        if (rel.IsNullable)
        {
            sb.AppendLine($"            item.{mappedByProperty} = null;");
        }
        else
        {
            // For non-nullable properties, we cannot assign null
            // The FK will be cleared, but the navigation property cannot be set to null
            // This is a design constraint - non-nullable relationships should not be removed this way
            sb.AppendLine($"            // Note: {mappedByProperty} is non-nullable, skipping null assignment");
        }
        sb.AppendLine();
        // Only clear FK if the target entity has the FK property
        if (rel.HasForeignKeyProperty)
        {
            sb.AppendLine("            // Also clear FK if exists");
            var fkPropertyName = $"{mappedByProperty}Id";
            sb.AppendLine($"            item.{fkPropertyName} = 0;");
        }
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void GenerateValidationMethods(StringBuilder sb, EntityRelationshipInfo entity)
    {
        sb.AppendLine();
        sb.AppendLine("    #region Validation Methods");
        sb.AppendLine();

        // Use fully qualified names
        var entityFullName = $"{entity.Namespace}.{entity.EntityName}";

        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Validates that all bidirectional relationships on {entity.EntityName} are consistent.");
        sb.AppendLine("    /// Throws InvalidOperationException if inconsistencies are detected.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static void ValidateRelationshipConsistency({entityFullName} entity)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (entity == null) throw new System.ArgumentNullException(nameof(entity));");
        sb.AppendLine();

        foreach (var rel in entity.BidirectionalRelationships)
        {
            if (rel.IsOwnerSide)
            {
                var fkProperty = $"{rel.PropertyName}Id";
                if (entity.ForeignKeyProperties.ContainsKey(fkProperty))
                {
                    sb.AppendLine($"        // Validate {rel.PropertyName} consistency");
                    sb.AppendLine($"        if (entity.{rel.PropertyName} != null)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            var expectedFk = entity.{rel.PropertyName}.Id;");
                    sb.AppendLine($"            if (entity.{fkProperty} != expectedFk)");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                throw new System.InvalidOperationException(");
                    sb.AppendLine($"                    $\"Bidirectional relationship inconsistency: {rel.PropertyName}Id ({{entity.{fkProperty}}}) does not match {rel.PropertyName}.Id ({{expectedFk}})\");");
                    sb.AppendLine("            }");
                    sb.AppendLine("        }");
                    sb.AppendLine($"        else if (entity.{fkProperty} != 0)");
                    sb.AppendLine("        {");
                    sb.AppendLine($"            throw new System.InvalidOperationException(");
                    sb.AppendLine($"                $\"Bidirectional relationship inconsistency: {rel.PropertyName}Id is {{entity.{fkProperty}}} but {rel.PropertyName} is null\");");
                    sb.AppendLine("        }");
                    sb.AppendLine();
                }
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    #endregion");
    }

    /// <summary>
    /// Gets the nullability of a property by checking its NullableAnnotation.
    /// </summary>
    private static bool GetPropertyNullability(INamedTypeSymbol classSymbol, string propertyName)
    {
        var propertySymbol = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == propertyName);
        return propertySymbol?.NullableAnnotation == NullableAnnotation.Annotated;
    }

    /// <summary>
    /// Gets the nullability of the owner-side property referenced by MappedBy.
    /// Used when processing inverse-side relationships to determine if null assignment is allowed.
    /// </summary>
    private static bool GetOwnerSidePropertyNullability(SemanticModel model, string ownerEntityTypeName, string mappedByPropertyName)
    {
        var ownerEntityType = model.Compilation.GetTypeByMetadataName(ownerEntityTypeName);
        if (ownerEntityType == null)
            return false; // Default to non-nullable if we can't determine

        var ownerPropertySymbol = ownerEntityType.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name == mappedByPropertyName);

        return ownerPropertySymbol?.NullableAnnotation == NullableAnnotation.Annotated;
    }

    /// <summary>
    /// Finds the inverse collection property name by looking for OneToMany relationships
    /// on the target entity that have MappedBy pointing to the specified property.
    /// </summary>
    private static string? FindInverseCollectionProperty(SemanticModel model, string targetEntityTypeName, string propertyName)
    {
        var targetEntityType = model.Compilation.GetTypeByMetadataName(targetEntityTypeName);
        if (targetEntityType == null)
            return null;

        var targetRelationships = MetadataExtractor.ExtractRelationships(targetEntityType);
        var inverseRel = targetRelationships.FirstOrDefault(r =>
            r.Type == "OneToMany" &&
            !string.IsNullOrEmpty(r.MappedBy) &&
            r.MappedBy == propertyName);

        return inverseRel?.PropertyName;
    }
}

internal class EntityRelationshipInfo
{
    public string EntityName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<BidirectionalRelationship> BidirectionalRelationships { get; set; } = new();
    public Dictionary<string, string> ForeignKeyProperties { get; set; } = new();
}

internal class BidirectionalRelationship
{
    public string PropertyName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public string? MappedBy { get; set; }
    public bool IsInverseSide { get; set; }
    public bool IsOwnerSide { get; set; }
    public bool IsCollection { get; set; }
    public bool IsNullable { get; set; }
    public string? InverseCollectionProperty { get; set; }
    public bool HasForeignKeyProperty { get; set; }
}
