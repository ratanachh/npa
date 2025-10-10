using NPA.Core.Core;

namespace NPA.Core.Metadata;

/// <summary>
/// Contains metadata information about composite keys.
/// </summary>
public sealed class CompositeKeyMetadata
{
    /// <summary>
    /// Gets or sets the list of properties that make up the composite key.
    /// </summary>
    public IList<PropertyMetadata> KeyProperties { get; set; } = new List<PropertyMetadata>();

    /// <summary>
    /// Gets the list of property names in the composite key.
    /// </summary>
    public IList<string> KeyNames => KeyProperties.Select(p => p.PropertyName).ToList();

    /// <summary>
    /// Gets the list of property types in the composite key.
    /// </summary>
    public IList<Type> KeyTypes => KeyProperties.Select(p => p.PropertyType).ToList();

    /// <summary>
    /// Gets the database column names for the composite key.
    /// </summary>
    public string[] KeyColumns => KeyProperties.Select(p => p.ColumnName).ToArray();

    /// <summary>
    /// Gets a value indicating whether this represents a composite key (more than one property).
    /// </summary>
    public bool IsCompositeKey => KeyProperties.Count > 1;

    /// <summary>
    /// Gets the number of properties in the composite key.
    /// </summary>
    public int Count => KeyProperties.Count;

    /// <summary>
    /// Creates a CompositeKey from an entity instance.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <returns>A CompositeKey with values extracted from the entity.</returns>
    public CompositeKey CreateCompositeKey(object entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var compositeKey = new CompositeKey();
        var entityType = entity.GetType();

        foreach (var property in KeyProperties)
        {
            var propertyInfo = entityType.GetProperty(property.PropertyName);
            if (propertyInfo == null)
                throw new InvalidOperationException($"Property '{property.PropertyName}' not found on entity type '{entityType.Name}'");

            var value = propertyInfo.GetValue(entity);
            if (value != null)
            {
                compositeKey.SetValue(property.PropertyName, value);
            }
        }

        return compositeKey;
    }

    /// <summary>
    /// Creates a CompositeKey from a dictionary of values.
    /// </summary>
    /// <param name="values">The key values.</param>
    /// <returns>A CompositeKey with the specified values.</returns>
    public CompositeKey CreateCompositeKeyFromValues(Dictionary<string, object> values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var compositeKey = new CompositeKey();

        foreach (var property in KeyProperties)
        {
            if (values.TryGetValue(property.PropertyName, out var value))
            {
                compositeKey.SetValue(property.PropertyName, value);
            }
            else
            {
                throw new ArgumentException($"Missing key value for property '{property.PropertyName}'", nameof(values));
            }
        }

        return compositeKey;
    }

    /// <summary>
    /// Generates a WHERE clause for the composite key.
    /// </summary>
    /// <returns>SQL WHERE clause string.</returns>
    public string GenerateWhereClause()
    {
        var conditions = KeyProperties.Select(p => $"{p.ColumnName} = @{p.PropertyName}");
        return string.Join(" AND ", conditions);
    }

    /// <summary>
    /// Extracts parameters from a CompositeKey for SQL queries.
    /// </summary>
    /// <param name="compositeKey">The composite key.</param>
    /// <returns>Dictionary of parameters.</returns>
    public Dictionary<string, object?> ExtractParameters(CompositeKey compositeKey)
    {
        if (compositeKey == null)
            throw new ArgumentNullException(nameof(compositeKey));

        var parameters = new Dictionary<string, object?>();

        foreach (var property in KeyProperties)
        {
            var value = compositeKey.GetValue(property.PropertyName);
            parameters[property.PropertyName] = value;
        }

        return parameters;
    }

    /// <summary>
    /// Validates that all key components are present and non-null.
    /// </summary>
    /// <param name="compositeKey">The composite key to validate.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public bool Validate(CompositeKey compositeKey)
    {
        if (compositeKey == null)
            return false;

        if (compositeKey.Values.Count != KeyProperties.Count)
            return false;

        foreach (var property in KeyProperties)
        {
            if (!compositeKey.ContainsKey(property.PropertyName))
                return false;

            var value = compositeKey.GetValue(property.PropertyName);
            if (value == null)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a formatted string representation of the composite key properties.
    /// </summary>
    /// <returns>Formatted string.</returns>
    public override string ToString()
    {
        return $"CompositeKeyMetadata[{string.Join(", ", KeyNames)}]";
    }
}

