using NPA.Core.Metadata;

namespace NPA.Core.Core;

/// <summary>
/// Provides a fluent API for building composite keys.
/// </summary>
public sealed class CompositeKeyBuilder
{
    private readonly CompositeKey _compositeKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeKeyBuilder"/> class.
    /// </summary>
    public CompositeKeyBuilder()
    {
        _compositeKey = new CompositeKey();
    }

    /// <summary>
    /// Adds a key component to the composite key.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The property value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public CompositeKeyBuilder WithKey(string propertyName, object value)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("Property name cannot be null or empty", nameof(propertyName));
        
        if (value == null)
            throw new ArgumentNullException(nameof(value), $"Value for property '{propertyName}' cannot be null");

        _compositeKey.SetValue(propertyName, value);
        return this;
    }

    /// <summary>
    /// Builds the composite key.
    /// </summary>
    /// <returns>The built composite key.</returns>
    public CompositeKey Build()
    {
        if (_compositeKey.Values.Count == 0)
            throw new InvalidOperationException("Composite key must have at least one component");

        return _compositeKey;
    }

    /// <summary>
    /// Creates a CompositeKeyBuilder from an entity and its metadata.
    /// </summary>
    /// <param name="entity">The entity instance.</param>
    /// <param name="metadata">The entity metadata.</param>
    /// <returns>A CompositeKeyBuilder with values from the entity.</returns>
    public static CompositeKeyBuilder FromEntity(object entity, EntityMetadata metadata)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        if (metadata == null)
            throw new ArgumentNullException(nameof(metadata));

        if (!metadata.HasCompositeKey || metadata.CompositeKeyMetadata == null)
            throw new InvalidOperationException($"Entity {metadata.EntityType.Name} does not have a composite key");

        var builder = new CompositeKeyBuilder();
        var entityType = entity.GetType();

        foreach (var keyProperty in metadata.CompositeKeyMetadata.KeyProperties)
        {
            var propertyInfo = entityType.GetProperty(keyProperty.PropertyName);
            if (propertyInfo == null)
                throw new InvalidOperationException($"Property '{keyProperty.PropertyName}' not found on entity type '{entityType.Name}'");

            var value = propertyInfo.GetValue(entity);
            if (value != null)
            {
                builder.WithKey(keyProperty.PropertyName, value);
            }
        }

        return builder;
    }

    /// <summary>
    /// Creates a fluent builder for a composite key.
    /// </summary>
    /// <returns>A new CompositeKeyBuilder instance.</returns>
    public static CompositeKeyBuilder Create()
    {
        return new CompositeKeyBuilder();
    }
}

