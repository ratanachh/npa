using NPA.Core.Metadata;

namespace NPA.Core.Query;

/// <summary>
/// Generates database-specific SQL from parsed queries.
/// </summary>
public class SqlGenerator : ISqlGenerator
{
    /// <inheritdoc />
    public string Generate(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        return parsedQuery.Type switch
        {
            QueryType.Select => GenerateSelect(parsedQuery, entityMetadata),
            QueryType.Update => GenerateUpdate(parsedQuery, entityMetadata),
            QueryType.Delete => GenerateDelete(parsedQuery, entityMetadata),
            _ => throw new ArgumentException($"Unsupported query type: {parsedQuery.Type}")
        };
    }

    /// <inheritdoc />
    public string GenerateSelect(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new System.Text.StringBuilder();
        
        // SELECT clause
        sql.Append("SELECT ");
        
        // Check if this is a COUNT query by looking at the original CPQL
        if (IsCountQuery(parsedQuery))
        {
            var primaryKeyColumn = entityMetadata.Properties[entityMetadata.PrimaryKeyProperty].ColumnName;
            sql.Append($"COUNT({parsedQuery.Alias}.{primaryKeyColumn})");
        }
        else
        {
            sql.Append(GenerateSelectColumns(entityMetadata, parsedQuery.Alias));
        }
        
        sql.Append(" FROM ");
        sql.Append(GetTableName(entityMetadata));
        sql.Append(" ");
        sql.Append(parsedQuery.Alias);

        // WHERE clause
        if (!string.IsNullOrEmpty(parsedQuery.WhereClause))
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateWhereClause(parsedQuery.WhereClause, entityMetadata, parsedQuery.Alias));
        }

        // ORDER BY clause (only for non-COUNT queries)
        if (!string.IsNullOrEmpty(parsedQuery.OrderByClause) && !IsCountQuery(parsedQuery))
        {
            sql.Append(" ORDER BY ");
            sql.Append(GenerateOrderByClause(parsedQuery.OrderByClause, entityMetadata, parsedQuery.Alias));
        }

        return sql.ToString();
    }

    /// <inheritdoc />
    public string GenerateUpdate(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new System.Text.StringBuilder();
        
        sql.Append("UPDATE ");
        sql.Append(GetTableName(entityMetadata));
        sql.Append(" SET ");
        
        // Debug: Log the SET clause processing
        var setClause = GenerateSetClause(parsedQuery.SetClause!, entityMetadata);
        sql.Append(setClause);

        if (!string.IsNullOrEmpty(parsedQuery.WhereClause))
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateWhereClause(parsedQuery.WhereClause, entityMetadata));
        }

        return sql.ToString();
    }

    /// <inheritdoc />
    public string GenerateDelete(ParsedQuery parsedQuery, EntityMetadata entityMetadata)
    {
        var sql = new System.Text.StringBuilder();
        
        sql.Append("DELETE FROM ");
        sql.Append(GetTableName(entityMetadata));

        if (!string.IsNullOrEmpty(parsedQuery.WhereClause))
        {
            sql.Append(" WHERE ");
            sql.Append(GenerateWhereClause(parsedQuery.WhereClause, entityMetadata));
        }

        return sql.ToString();
    }

    /// <inheritdoc />
    public string GenerateWhereClause(string? whereClause, EntityMetadata entityMetadata)
    {
        if (string.IsNullOrEmpty(whereClause))
            return string.Empty;

        return ResolvePropertyNames(whereClause, entityMetadata);
    }

    /// <inheritdoc />
    public string GenerateOrderByClause(string? orderByClause, EntityMetadata entityMetadata)
    {
        if (string.IsNullOrEmpty(orderByClause))
            return string.Empty;

        return ResolvePropertyNames(orderByClause, entityMetadata);
    }

    private string GenerateWhereClause(string? whereClause, EntityMetadata entityMetadata, string alias)
    {
        if (string.IsNullOrEmpty(whereClause))
            return string.Empty;

        return ResolvePropertyNames(whereClause, entityMetadata, alias);
    }

    private string GenerateOrderByClause(string? orderByClause, EntityMetadata entityMetadata, string alias)
    {
        if (string.IsNullOrEmpty(orderByClause))
            return string.Empty;

        return ResolvePropertyNames(orderByClause, entityMetadata, alias);
    }

    private string GenerateSelectColumns(EntityMetadata entityMetadata)
    {
        var columns = entityMetadata.Properties.Values
            .Select(p => $"{entityMetadata.TableName}.{p.ColumnName}")
            .ToList();

        return string.Join(", ", columns);
    }

    private string GenerateSelectColumns(EntityMetadata entityMetadata, string alias)
    {
        var columns = entityMetadata.Properties.Values
            .Select(p => $"{alias}.{p.ColumnName} AS {p.PropertyName}")
            .ToList();

        return string.Join(", ", columns);
    }

    private string GenerateSetClause(string setClause, EntityMetadata entityMetadata)
    {
        return ResolvePropertyNames(setClause, entityMetadata);
    }

    private string GetTableName(EntityMetadata entityMetadata)
    {
        return entityMetadata.FullTableName;
    }

    private string ResolvePropertyNames(string clause, EntityMetadata entityMetadata)
    {
        var resolvedClause = clause;

        // Replace alias.property names with column names (for UPDATE/DELETE queries)
        foreach (var property in entityMetadata.Properties.Values)
        {
            // Handle alias.property pattern (e.g., "u.Username" -> "username")
            var aliasPattern = $@"\b\w+\.{property.PropertyName}\b";
            resolvedClause = System.Text.RegularExpressions.Regex.Replace(
                resolvedClause, 
                aliasPattern, 
                property.ColumnName, 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Convert CPQL parameter syntax (:paramName) to SQL parameter syntax (@paramName)
        resolvedClause = System.Text.RegularExpressions.Regex.Replace(
            resolvedClause,
            @":(\w+)",
            "@$1",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        return resolvedClause;
    }

    private string ResolvePropertyNames(string clause, EntityMetadata entityMetadata, string alias)
    {
        var resolvedClause = clause;

        // Replace alias.property names with alias.column names
        foreach (var property in entityMetadata.Properties.Values)
        {
            var pattern = $@"\b{alias}\.{property.PropertyName}\b";
            resolvedClause = System.Text.RegularExpressions.Regex.Replace(
                resolvedClause, 
                pattern, 
                $"{alias}.{property.ColumnName}", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        // Convert CPQL parameter syntax (:paramName) to SQL parameter syntax (@paramName)
        resolvedClause = System.Text.RegularExpressions.Regex.Replace(
            resolvedClause,
            @":(\w+)",
            "@$1",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        return resolvedClause;
    }

    private bool IsCountQuery(ParsedQuery parsedQuery)
    {
        return !string.IsNullOrEmpty(parsedQuery.OriginalCpql) && 
               parsedQuery.OriginalCpql.ToUpperInvariant().Contains("COUNT(");
    }
}
