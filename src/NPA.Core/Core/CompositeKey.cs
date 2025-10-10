using System.Text;

namespace NPA.Core.Core;

/// <summary>
/// Represents a composite key for entities with multiple primary key columns.
/// </summary>
public sealed class CompositeKey : IEquatable<CompositeKey>
{
    private readonly Dictionary<string, object> _values;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeKey"/> class.
    /// </summary>
    public CompositeKey()
    {
        _values = new Dictionary<string, object>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeKey"/> class with the specified values.
    /// </summary>
    /// <param name="values">The key values.</param>
    public CompositeKey(Dictionary<string, object> values)
    {
        _values = new Dictionary<string, object>(values ?? throw new ArgumentNullException(nameof(values)));
    }

    /// <summary>
    /// Gets the value for the specified property name.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns>The value for the property.</returns>
    public object? GetValue(string propertyName)
    {
        return _values.TryGetValue(propertyName, out var value) ? value : null;
    }

    /// <summary>
    /// Sets the value for the specified property name.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string propertyName, object? value)
    {
        if (value != null)
        {
            _values[propertyName] = value;
        }
        else
        {
            _values.Remove(propertyName);
        }
    }

    /// <summary>
    /// Gets all the key values.
    /// </summary>
    public IReadOnlyDictionary<string, object> Values => _values;

    /// <summary>
    /// Determines whether the composite key contains the specified property name.
    /// </summary>
    /// <param name="propertyName">The property name.</param>
    /// <returns>True if the composite key contains the property; otherwise, false.</returns>
    public bool ContainsKey(string propertyName)
    {
        return _values.ContainsKey(propertyName);
    }

    /// <summary>
    /// Gets the number of key components.
    /// </summary>
    public int Count => _values.Count;

    /// <inheritdoc />
    public bool Equals(CompositeKey? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_values.Count != other._values.Count) return false;

        foreach (var kvp in _values)
        {
            if (!other._values.TryGetValue(kvp.Key, out var otherValue) ||
                !Equals(kvp.Value, otherValue))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as CompositeKey);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var kvp in _values.OrderBy(x => x.Key))
        {
            hash.Add(kvp.Key);
            hash.Add(kvp.Value);
        }
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.Append("CompositeKey(");
        sb.Append(string.Join(", ", _values.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        sb.Append(")");
        return sb.ToString();
    }
}
