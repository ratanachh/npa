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
        foreach (var property in properties)
        {
            var propertyMetadata = BuildPropertyMetadata(property);
            metadata.Properties[property.Name] = propertyMetadata;

            // Set primary key if this property has the Id attribute
            if (propertyMetadata.IsPrimaryKey)
            {
                metadata.PrimaryKeyProperty = property.Name;
            }
        }

        if (string.IsNullOrEmpty(metadata.PrimaryKeyProperty))
        {
            throw new InvalidOperationException($"Entity {entityType.Name} must have a property marked with [Id] attribute.");
        }

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
                // Default to Identity for primary keys
                metadata.GenerationType = Annotations.GenerationType.Identity;
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
