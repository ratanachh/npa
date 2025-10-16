using System.Reflection;
using System.Text;
using NPA.Core.Annotations;

namespace NPA.Core.Metadata;

/// <summary>
/// Provides entity metadata by analyzing entity types and their attributes.
/// </summary>
public sealed class MetadataProvider : IMetadataProvider
{
    private readonly Dictionary<Type, EntityMetadata> _metadataCache = new();
    private readonly Dictionary<string, Type> _nameToTypeCache = new();

    /// <inheritdoc />
    public EntityMetadata GetEntityMetadata<T>()
    {
        return GetEntityMetadata(typeof(T));
    }

    /// <inheritdoc />
    public EntityMetadata GetEntityMetadata(Type entityType)
    {
        if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

        if (!IsEntity(entityType))
            throw new ArgumentException($"Type {entityType.Name} is not marked as an entity.", nameof(entityType));

        if (_metadataCache.TryGetValue(entityType, out var cachedMetadata))
            return cachedMetadata;

        var metadata = BuildEntityMetadata(entityType);
        _metadataCache[entityType] = metadata;
        _nameToTypeCache[entityType.Name] = entityType;
        return metadata;
    }

    /// <inheritdoc />
    public EntityMetadata GetEntityMetadata(string entityName)
    {
        if (string.IsNullOrEmpty(entityName))
            throw new ArgumentNullException(nameof(entityName));

        if (_nameToTypeCache.TryGetValue(entityName, out var cachedType))
        {
            return GetEntityMetadata(cachedType);
        }

        // Scan assemblies to find the type (with performance optimization)
        var entityType = FindEntityTypeByName(entityName);

        if (entityType == null)
        {
            throw new ArgumentException($"No entity with the name '{entityName}' could be found in the current AppDomain.", nameof(entityName));
        }

        // Cache the type for future lookups
        _nameToTypeCache[entityName] = entityType;
        return GetEntityMetadata(entityType);
    }

    private static Type? FindEntityTypeByName(string entityName)
    {
        // Cache assembly types to avoid repeated scanning
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    if (type.Name == entityName && IsEntityStatic(type))
                    {
                        return type;
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't be fully loaded
                continue;
            }
            catch (Exception)
            {
                // Skip assemblies that cause other reflection errors
                continue;
            }
        }
        
        return null;
    }

    private static bool IsEntityStatic(Type type)
    {
        if (type == null)
            return false;

        return type.GetCustomAttribute<EntityAttribute>() != null;
    }

    /// <inheritdoc />
    public bool IsEntity(Type type)
    {
        if (type == null)
            return false;

        return type.GetCustomAttribute<EntityAttribute>() != null;
    }

    private EntityMetadata BuildEntityMetadata(Type entityType)
    {
        var metadata = new EntityMetadata
        {
            EntityType = entityType
        };

        var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
        metadata.TableName = tableAttribute?.Name ?? entityType.Name;
        metadata.SchemaName = tableAttribute?.Schema;

        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var primaryKeyProperties = new List<PropertyMetadata>();

        foreach (var property in properties)
        {
            // Ignore properties that are marked as relationships
            if (property.GetCustomAttribute<OneToOneAttribute>() != null ||
                property.GetCustomAttribute<OneToManyAttribute>() != null ||
                property.GetCustomAttribute<ManyToOneAttribute>() != null ||
                property.GetCustomAttribute<ManyToManyAttribute>() != null)
            {
                continue;
            }

            var propertyMetadata = BuildPropertyMetadata(property);
            metadata.Properties[property.Name] = propertyMetadata;

            if (propertyMetadata.IsPrimaryKey)
            {
                primaryKeyProperties.Add(propertyMetadata);
            }
        }

        if (primaryKeyProperties.Count == 0)
        {
            throw new InvalidOperationException($"Entity {entityType.Name} must have at least one property marked with [Id] attribute.");
        }
        else if (primaryKeyProperties.Count == 1)
        {
            metadata.PrimaryKeyProperty = primaryKeyProperties[0].PropertyName;
        }
        else
        {
            metadata.PrimaryKeyProperty = primaryKeyProperties[0].PropertyName; // Compatibility
            metadata.CompositeKeyMetadata = new CompositeKeyMetadata { KeyProperties = primaryKeyProperties };
        }

        BuildRelationships(entityType, metadata);

        return metadata;
    }

    private PropertyMetadata BuildPropertyMetadata(PropertyInfo property)
    {
        var metadata = new PropertyMetadata
        {
            PropertyInfo = property,
            PropertyName = property.Name,
            PropertyType = property.PropertyType
        };

        var idAttribute = property.GetCustomAttribute<IdAttribute>();
        if (idAttribute != null)
        {
            metadata.IsPrimaryKey = true;
            var generatedValueAttribute = property.GetCustomAttribute<GeneratedValueAttribute>();
            metadata.GenerationType = generatedValueAttribute?.Strategy ?? GenerationType.None;
        }

        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        metadata.ColumnName = columnAttribute?.Name ?? ConvertToSnakeCase(property.Name);
        if (columnAttribute != null)
        {
            metadata.IsNullable = columnAttribute.IsNullable;
            metadata.IsUnique = columnAttribute.IsUnique;
            metadata.Length = columnAttribute.Length;
            metadata.Precision = columnAttribute.Precision;
            metadata.Scale = columnAttribute.Scale;
            metadata.TypeName = columnAttribute.TypeName;
        }

        return metadata;
    }

    private void BuildRelationships(Type entityType, EntityMetadata metadata)
    {
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            var oneToOne = property.GetCustomAttribute<OneToOneAttribute>();
            if (oneToOne != null)
            {
                var relationship = new RelationshipMetadata
                {
                    PropertyName = property.Name,
                    RelationshipType = RelationshipType.OneToOne,
                    TargetEntityType = property.PropertyType,
                    MappedBy = oneToOne.MappedBy,
                    CascadeType = oneToOne.Cascade,
                    FetchType = oneToOne.Fetch,
                    IsOptional = oneToOne.Optional,
                    IsOwner = string.IsNullOrEmpty(oneToOne.MappedBy)
                };

                if (relationship.IsOwner)
                {
                    var joinColumn = property.GetCustomAttribute<JoinColumnAttribute>();
                    relationship.JoinColumn = new JoinColumnMetadata
                    {
                        Name = joinColumn?.Name ?? ConvertToSnakeCase(property.Name) + "_id",
                        ReferencedColumnName = joinColumn?.ReferencedColumnName ?? "id",
                        Unique = joinColumn?.Unique ?? false,
                        Nullable = joinColumn?.Nullable ?? oneToOne.Optional,
                        Insertable = joinColumn?.Insertable ?? true,
                        Updatable = joinColumn?.Updatable ?? true
                    };
                }

                metadata.Relationships[property.Name] = relationship;
                continue;
            }

            var oneToMany = property.GetCustomAttribute<OneToManyAttribute>();
            if (oneToMany != null)
            {
                var relationship = new RelationshipMetadata
                {
                    PropertyName = property.Name,
                    RelationshipType = RelationshipType.OneToMany,
                    TargetEntityType = GetCollectionElementType(property.PropertyType),
                    MappedBy = oneToMany.MappedBy,
                    CascadeType = oneToMany.Cascade,
                    FetchType = oneToMany.Fetch,
                    OrphanRemoval = oneToMany.OrphanRemoval,
                    IsOwner = string.IsNullOrEmpty(oneToMany.MappedBy)
                };
                metadata.Relationships[property.Name] = relationship;
                continue;
            }

            var manyToOne = property.GetCustomAttribute<ManyToOneAttribute>();
            if (manyToOne != null)
            {
                var relationship = new RelationshipMetadata
                {
                    PropertyName = property.Name,
                    RelationshipType = RelationshipType.ManyToOne,
                    TargetEntityType = property.PropertyType,
                    CascadeType = manyToOne.Cascade,
                    FetchType = manyToOne.Fetch,
                    IsOptional = manyToOne.Optional,
                    IsOwner = true
                };

                var joinColumn = property.GetCustomAttribute<JoinColumnAttribute>();
                relationship.JoinColumn = new JoinColumnMetadata
                {
                    Name = joinColumn?.Name ?? ConvertToSnakeCase(property.Name) + "_id",
                    ReferencedColumnName = joinColumn?.ReferencedColumnName ?? "id",
                    Unique = joinColumn?.Unique ?? false,
                    Nullable = joinColumn?.Nullable ?? manyToOne.Optional,
                    Insertable = joinColumn?.Insertable ?? true,
                    Updatable = joinColumn?.Updatable ?? true
                };

                metadata.Relationships[property.Name] = relationship;
                continue;
            }

            var manyToMany = property.GetCustomAttribute<ManyToManyAttribute>();
            if (manyToMany != null)
            {
                var relationship = new RelationshipMetadata
                {
                    PropertyName = property.Name,
                    RelationshipType = RelationshipType.ManyToMany,
                    TargetEntityType = GetCollectionElementType(property.PropertyType),
                    MappedBy = manyToMany.MappedBy,
                    CascadeType = manyToMany.Cascade,
                    FetchType = manyToMany.Fetch,
                    IsOwner = string.IsNullOrEmpty(manyToMany.MappedBy)
                };

                if (relationship.IsOwner)
                {
                    var joinTable = property.GetCustomAttribute<JoinTableAttribute>();
                    if (joinTable != null)
                    {
                        relationship.JoinTable = new JoinTableMetadata
                        {
                            Name = joinTable.Name,
                            Schema = joinTable.Schema,
                            JoinColumns = new List<string>(joinTable.JoinColumns),
                            InverseJoinColumns = new List<string>(joinTable.InverseJoinColumns)
                        };
                    }
                    else
                    {
                        var targetType = relationship.TargetEntityType;
                        relationship.JoinTable = new JoinTableMetadata
                        {
                            Name = $"{ConvertToSnakeCase(entityType.Name)}_{ConvertToSnakeCase(targetType.Name)}",
                            JoinColumns = new List<string> { ConvertToSnakeCase(entityType.Name) + "_id" },
                            InverseJoinColumns = new List<string> { ConvertToSnakeCase(targetType.Name) + "_id" }
                        };
                    }
                }

                metadata.Relationships[property.Name] = relationship;
            }
        }
    }

    private static Type GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }

        if (collectionType.IsArray)
        {
            return collectionType.GetElementType()!;
        }

        throw new InvalidOperationException($"Unable to determine element type for collection type: {collectionType.Name}");
    }

    private static string ConvertToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            var currentChar = input[i];
            if (char.IsUpper(currentChar))
            {
                if (i > 0)
                    result.Append('_');
                result.Append(char.ToLower(currentChar));
            }
            else
            {
                result.Append(currentChar);
            }
        }

        return result.ToString();
    }
}
