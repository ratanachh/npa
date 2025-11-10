namespace NPA.Core.Annotations;

/// <summary>
/// Marks an interface for automatic repository implementation generation.
/// The source generator will create a concrete implementation with method bodies.
/// The entity type is extracted from the IRepository&lt;TEntity, TKey&gt; interface.
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class RepositoryAttribute : Attribute
{
}

