namespace NPA.Core.Annotations;

/// <summary>
/// Specifies that a repository method executes a stored procedure.
/// </summary>
/// <example>
/// <code>
/// [StoredProcedure("sp_GetActiveUsers")]
/// Task&lt;IEnumerable&lt;User&gt;&gt; GetActiveUsersAsync();
/// 
/// [StoredProcedure("sp_UpdateUserStatus")]
/// Task&lt;int&gt; UpdateUserStatusAsync(long userId, string status);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method)]
public sealed class StoredProcedureAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the stored procedure to execute.
    /// </summary>
    public string ProcedureName { get; }

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; set; }

    /// <summary>
    /// Gets or sets the schema name for the stored procedure.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StoredProcedureAttribute"/> class.
    /// </summary>
    /// <param name="procedureName">The name of the stored procedure.</param>
    public StoredProcedureAttribute(string procedureName)
    {
        if (string.IsNullOrWhiteSpace(procedureName))
            throw new ArgumentException("Procedure name cannot be null or empty", nameof(procedureName));
        
        ProcedureName = procedureName;
    }
}
