namespace NPA.Core.Annotations;

/// <summary>
/// Marks a property as the primary key of an entity.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IdAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IdAttribute"/> class.
    /// </summary>
    public IdAttribute()
    {
    }
}
