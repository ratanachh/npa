namespace NPA.Design.Models;

/// <summary>
/// Contains metadata information about named queries defined on an entity.
/// </summary>
public class NamedQueryInfo
{
    /// <summary>
    /// Gets or sets the unique name of the query.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the query string (CPQL or SQL).
    /// </summary>
    public string Query { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets a value indicating whether this is a native SQL query.
    /// </summary>
    public bool NativeQuery { get; set; }
    
    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int? CommandTimeout { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether to buffer the results.
    /// </summary>
    public bool Buffered { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the description of what this query does.
    /// </summary>
    public string? Description { get; set; }
}

