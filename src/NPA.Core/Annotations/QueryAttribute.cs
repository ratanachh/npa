namespace NPA.Core.Annotations;

/// <summary>
/// Specifies a custom SQL query for a repository method.
/// The query can include named parameters that match method parameters.
/// </summary>
/// <example>
/// <code>
/// [Query("SELECT * FROM users WHERE email = @email")]
/// Task&lt;User?&gt; FindByEmailAsync(string email);
/// 
/// [Query("SELECT * FROM orders WHERE user_id = @userId AND status = @status")]
/// Task&lt;IEnumerable&lt;Order&gt;&gt; FindByUserAndStatusAsync(long userId, string status);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class QueryAttribute : Attribute
{
    /// <summary>
    /// Gets the SQL query to execute.
    /// </summary>
    public string Sql { get; }

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
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    /// <param name="sql">The SQL query to execute.</param>
    public QueryAttribute(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL query cannot be null or empty", nameof(sql));
        
        Sql = sql;
    }
}
