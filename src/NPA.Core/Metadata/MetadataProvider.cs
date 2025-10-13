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
        return metadata;
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

        // Get table information
        var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
        if (tableAttribute != null)
        {
            metadata.TableName = tableAttribute.Name;
            metadata.SchemaName = tableAttribute.Schema;
        }
        else
        {
            // Default to class name if no Table attribute
            metadata.TableName = entityType.Name;
        }

        // Get property metadata
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var primaryKeyProperties = new List<PropertyMetadata>();
        
        foreach (var property in properties)
        {
            var propertyMetadata = BuildPropertyMetadata(property);
            metadata.Properties[property.Name] = propertyMetadata;

            // Collect all primary key properties
            if (propertyMetadata.IsPrimaryKey)
            {
                primaryKeyProperties.Add(propertyMetadata);
            }
        }

        // Validate and configure primary key
        if (primaryKeyProperties.Count == 0)
        {
            throw new InvalidOperationException($"Entity {entityType.Name} must have at least one property marked with [Id] attribute.");
        }
        else if (primaryKeyProperties.Count == 1)
        {
            // Single primary key
            metadata.PrimaryKeyProperty = primaryKeyProperties[0].PropertyName;
        }
        else
        {
            // Composite key (multiple [Id] attributes)
            metadata.PrimaryKeyProperty = primaryKeyProperties[0].PropertyName; // Set first as primary for compatibility
            metadata.CompositeKeyMetadata = new CompositeKeyMetadata
            {
                KeyProperties = primaryKeyProperties
            };
        }

        // Build relationships after properties are set
        BuildRelationships(entityType, metadata);

        return metadata;
    }

    private PropertyMetadata BuildPropertyMetadata(PropertyInfo property)
    {
        var metadata = new PropertyMetadata
        {
            PropertyName = property.Name,
            PropertyType = property.PropertyType
        };

        // Check for Id attribute
        var idAttribute = property.GetCustomAttribute<IdAttribute>();
        if (idAttribute != null)
        {
            metadata.IsPrimaryKey = true;

            // Check for GeneratedValue attribute
            var generatedValueAttribute = property.GetCustomAttribute<GeneratedValueAttribute>();
            if (generatedValueAttribute != null)
            {
                metadata.GenerationType = generatedValueAttribute.Strategy;
            }
            else
            {
                // Default to None for primary keys when no [GeneratedValue] is specified
                metadata.GenerationType = GenerationType.None;
            }
        }

        // Check for Column attribute
        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        if (columnAttribute != null)
        {
            metadata.ColumnName = columnAttribute.Name;
            metadata.IsNullable = columnAttribute.IsNullable;
            metadata.IsUnique = columnAttribute.IsUnique;
            metadata.Length = columnAttribute.Length;
            metadata.Precision = columnAttribute.Precision;
            metadata.Scale = columnAttribute.Scale;
            metadata.TypeName = columnAttribute.TypeName;
        }
        else
        {
            // Default column name based on property name (convert to snake_case)
            metadata.ColumnName = ConvertToSnakeCase(property.Name);
        }

        return metadata;
    }

    private void BuildRelationships(Type entityType, EntityMetadata metadata)
    {
        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            // Check for OneToMany relationship
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

            // Check for ManyToOne relationship
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

                // Check for JoinColumn attribute
                var joinColumn = property.GetCustomAttribute<JoinColumnAttribute>();
                if (joinColumn != null)
                {
                    relationship.JoinColumn = new JoinColumnMetadata
                    {
                        Name = joinColumn.Name,
                        ReferencedColumnName = joinColumn.ReferencedColumnName,
                        Unique = joinColumn.Unique,
                        Nullable = joinColumn.Nullable,
                        Insertable = joinColumn.Insertable,
                        Updatable = joinColumn.Updatable
                    };
                }
                else
                {
                    // Default join column name: {property_name}_id
                    relationship.JoinColumn = new JoinColumnMetadata
                    {
                        Name = ConvertToSnakeCase(property.Name) + "_id",
                        ReferencedColumnName = "id",
                        Nullable = manyToOne.Optional
                    };
                }

                metadata.Relationships[property.Name] = relationship;
                continue;
            }

            // Check for ManyToMany relationship
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

                // Check for JoinTable attribute (only on owning side)
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
                        // Default join table name: {entity1}_{entity2}
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
        // Handle ICollection<T>, IList<T>, List<T>, etc.
        if (collectionType.IsGenericType)
        {
            var genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }

        // Handle arrays
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
