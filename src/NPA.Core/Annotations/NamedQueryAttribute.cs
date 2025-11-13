namespace NPA.Core.Annotations;

/// <summary>
/// Defines a named query that can be referenced in repository methods.
/// Named queries are associated with an entity and can be reused across multiple repository methods.
/// </summary>
/// <example>
/// <code>
/// [Entity]
/// [Table("users")]
/// [NamedQuery("User.FindByEmailAsync", "SELECT u FROM User u WHERE u.Email = :email")]
/// [NamedQuery("User.FindActiveUsersAsync", "SELECT u FROM User u WHERE u.IsActive = true")]
/// public class User
/// {
///     [Id]
///     public long Id { get; set; }
///     
///     [Column("email")]
///     public string Email { get; set; }
///     
///     [Column("is_active")]
///     public bool IsActive { get; set; }
/// }
/// 
/// // In repository - methods are auto-matched by name:
/// [Repository]
/// public interface IUserRepository : IRepository&lt;User, long&gt;
/// {
///     // Auto-matches "User.FindByEmailAsync"
///     Task&lt;User?&gt; FindByEmailAsync(string email);
///     
///     // Auto-matches "User.FindActiveUsersAsync"
///     Task&lt;IEnumerable&lt;User&gt;&gt; FindActiveUsersAsync();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class NamedQueryAttribute : Attribute
{
    /// <summary>
    /// Gets the unique name of the query.
    /// Convention: EntityName.MethodName (e.g., "User.FindByEmailAsync" matches FindByEmailAsync() method).
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the CPQL or SQL query string.
    /// </summary>
    public string Query { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a native SQL query.
    /// When true, the query is executed as-is without CPQL to SQL conversion.
    /// Default is false (CPQL conversion enabled).
    /// </summary>
    public bool NativeQuery { get; set; } = false;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to buffer the results.
    /// Default is true for compatibility with Dapper.
    /// </summary>
    public bool Buffered { get; set; } = true;

    /// <summary>
    /// Gets or sets a description of what this query does.
    /// Used for documentation and code generation.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedQueryAttribute"/> class.
    /// </summary>
    /// <param name="name">The unique name of the query.</param>
    /// <param name="query">The CPQL or SQL query string.</param>
    /// <exception cref="ArgumentException">Thrown when name or query is null or empty.</exception>
    public NamedQueryAttribute(string name, string query)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Named query name cannot be null or empty", nameof(name));
        
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("Named query string cannot be null or empty", nameof(query));

        Name = name;
        Query = query;
    }
}
