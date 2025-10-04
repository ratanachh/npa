namespace NPA.Core.Annotations;

/// <summary>
/// Marks a class as an entity that can be persisted to the database.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EntityAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EntityAttribute"/> class.
    /// </summary>
    public EntityAttribute()
    {
    }
}
