namespace NPA.Core.Providers;

/// <summary>
/// Defines the contract for type conversion between .NET types and database types.
/// </summary>
public interface ITypeConverter
{
    /// <summary>
    /// Converts a .NET value to a database-compatible value.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target database type.</param>
    /// <returns>The converted value.</returns>
    object? ConvertToDatabase(object? value, Type targetType);

    /// <summary>
    /// Converts a database value to a .NET-compatible value.
    /// </summary>
    /// <param name="value">The database value to convert.</param>
    /// <param name="targetType">The target .NET type.</param>
    /// <returns>The converted value.</returns>
    object? ConvertFromDatabase(object? value, Type targetType);

    /// <summary>
    /// Determines if the specified type is supported by this converter.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is supported; otherwise, false.</returns>
    bool SupportsType(Type type);

    /// <summary>
    /// Gets the default database value for the specified .NET type.
    /// </summary>
    /// <param name="type">The .NET type.</param>
    /// <returns>The default database value.</returns>
    object? GetDefaultValue(Type type);

    /// <summary>
    /// Converts a .NET type to the corresponding database type name.
    /// </summary>
    /// <param name="dotNetType">The .NET type.</param>
    /// <param name="length">The length (optional).</param>
    /// <param name="precision">The precision (optional).</param>
    /// <param name="scale">The scale (optional).</param>
    /// <returns>The database type name.</returns>
    string GetDatabaseTypeName(Type dotNetType, int? length = null, int? precision = null, int? scale = null);

    /// <summary>
    /// Determines if the specified value requires special handling for null values.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="type">The target type.</param>
    /// <returns>True if special null handling is required; otherwise, false.</returns>
    bool RequiresSpecialNullHandling(object? value, Type type);
}