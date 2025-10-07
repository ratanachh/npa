using NPA.Core;

namespace NPA.Extensions;

/// <summary>
/// Provides extension methods for NPA entities and operations.
/// </summary>
public static class EntityExtensions
{
    /// <summary>
    /// Validates if an entity has all required properties set.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entity">The entity to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid<T>(this T entity) where T : class
    {
        // TODO: Implement validation logic
        return entity != null;
    }

    /// <summary>
    /// Converts an entity to a dictionary representation.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <param name="entity">The entity to convert</param>
    /// <returns>Dictionary representation of the entity</returns>
    public static Dictionary<string, object?> ToDictionary<T>(this T entity) where T : class
    {
        // TODO: Implement dictionary conversion
        var dict = new Dictionary<string, object?>();
        return dict;
    }
}