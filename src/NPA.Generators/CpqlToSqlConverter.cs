using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace NPA.Generators;

/// <summary>
/// Converts CPQL (Custom Persistence Query Language) style queries to standard SQL.
/// Supports Spring Data JPA style entity queries like "SELECT e FROM Entity e WHERE e.Property = :param"
/// and converts them to standard SQL like "SELECT * FROM Entity WHERE Property = @param"
/// </summary>
public static class CpqlToSqlConverter
{
    /// <summary>
    /// Converts a CPQL query to standard SQL.
    /// </summary>
    /// <param name="cpql">The CPQL query string</param>
    /// <param name="metadata">Entity metadata for table name and column mapping (optional) - for single entity queries</param>
    /// <param name="formatSql">Whether to format the SQL with line breaks for readability (default: false)</param>
    /// <returns>Converted SQL query</returns>
    public static string ConvertToSql(string cpql, EntityMetadataInfo? metadata = null, bool formatSql = false)
    {
        // For backward compatibility - convert single metadata to dictionary
        var metadataMap = metadata != null 
            ? new Dictionary<string, EntityMetadataInfo> { { metadata.Name, metadata } }
            : new Dictionary<string, EntityMetadataInfo>();
        
        return ConvertToSql(cpql, metadataMap, formatSql);
    }

    /// <summary>
    /// Converts a CPQL query to standard SQL with metadata for multiple entities (for JOIN queries).
    /// </summary>
    /// <param name="cpql">The CPQL query string</param>
    /// <param name="entitiesMetadata">Dictionary of entity metadata keyed by entity name (e.g., "Product", "Category")</param>
    /// <param name="formatSql">Whether to format the SQL with line breaks for readability (default: false)</param>
    /// <returns>Converted SQL query</returns>
    public static string ConvertToSql(string cpql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, bool formatSql = false)
    {
        if (string.IsNullOrWhiteSpace(cpql))
            return cpql;

        var sql = cpql;

        // Step 1: Convert parameter syntax from :param to @param
        sql = ConvertParameterSyntax(sql);

        // Step 2: Extract entity-to-alias mapping from the query
        var entityAliasMap = ExtractEntityAliasMapping(sql);
        
        // Detect query type (SELECT, INSERT, UPDATE, DELETE)
        var queryType = DetectQueryType(sql);
        
        if (queryType == "INSERT")
        {
            // Step 3: Convert INSERT INTO clause
            sql = ConvertInsertClause(sql, entitiesMetadata, entityAliasMap);
            
            // Step 4: No need to remove aliases for INSERT (no entity aliases in column list or VALUES)
        }
        else if (queryType == "UPDATE")
        {
            // Step 3: Convert UPDATE clause
            sql = ConvertUpdateClause(sql, entitiesMetadata, entityAliasMap);
            
            // Step 4: Remove entity alias from SET and WHERE clauses
            sql = RemoveEntityAliases(sql, entitiesMetadata, entityAliasMap);
        }
        else if (queryType == "DELETE")
        {
            // Step 3: Convert DELETE FROM clause
            sql = ConvertDeleteFromClause(sql, entitiesMetadata, entityAliasMap);
            
            // Step 4: Remove entity alias from WHERE clause
            sql = RemoveEntityAliases(sql, entitiesMetadata, entityAliasMap);
        }
        else // SELECT
        {
            // Step 3: Convert SELECT clause (e.g., "SELECT e" or "SELECT COUNT(e)" or "SELECT AVG(e.Property)")
            sql = ConvertSelectClause(sql, entitiesMetadata, entityAliasMap);

            // Step 4: Convert FROM clause (e.g., "FROM Entity e" to "FROM table_name")
            sql = ConvertFromClause(sql, entitiesMetadata, entityAliasMap);

            // Step 5: Remove entity alias from property references (e.g., "e.Property" to "column_name")
            sql = RemoveEntityAliases(sql, entitiesMetadata, entityAliasMap);

            // Step 6: Convert LIMIT (some databases use different syntax)
            // For now, keep LIMIT as is (works for MySQL, PostgreSQL)
            // SQL Server would need FETCH FIRST n ROWS ONLY or TOP n
        }

        // Step 7: Format the SQL for readability (if requested)
        if (formatSql)
        {
            sql = FormatSql(sql);
        }

        return sql;
    }

    /// <summary>
    /// Detects the type of query (SELECT, INSERT, UPDATE, DELETE)
    /// </summary>
    private static string DetectQueryType(string sql)
    {
        var trimmed = sql.TrimStart();
        if (trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
            return "INSERT";
        if (trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase))
            return "UPDATE";
        if (trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
            return "DELETE";
        return "SELECT";
    }

    /// <summary>
    /// Converts CPQL UPDATE clause to SQL UPDATE clause
    /// Example: "UPDATE User u SET u.Name = :name WHERE u.Id = :id"
    /// becomes: "UPDATE users SET name = @name WHERE id = @id"
    /// </summary>
    private static string ConvertUpdateClause(string cpql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap)
    {
        // Pattern: UPDATE EntityName alias SET ...
        var updatePattern = @"\bUPDATE\s+(\w+)\s+(\w+)\s+SET\b";
        cpql = Regex.Replace(cpql, updatePattern, match =>
        {
            var entityName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            
            // Get table name from metadata
            if (entitiesMetadata.TryGetValue(entityName, out var metadata))
            {
                return $"UPDATE {metadata.TableName} SET";
            }
            
            // Fallback: use entity name as table name
            return $"UPDATE {entityName.ToLowerInvariant()} SET";
        }, RegexOptions.IgnoreCase);
        
        return cpql;
    }

    /// <summary>
    /// Converts CPQL DELETE FROM clause to SQL DELETE FROM clause
    /// Example: "DELETE FROM User u WHERE u.IsActive = false"
    /// becomes: "DELETE FROM users WHERE is_active = false"
    /// </summary>
    private static string ConvertDeleteFromClause(string cpql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap)
    {
        // Pattern: DELETE FROM EntityName alias WHERE ...
        var deletePattern = @"\bDELETE\s+FROM\s+(\w+)\s+(\w+)(?=\s+WHERE|\s*$)";
        cpql = Regex.Replace(cpql, deletePattern, match =>
        {
            var entityName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            
            // Get table name from metadata
            if (entitiesMetadata.TryGetValue(entityName, out var metadata))
            {
                return $"DELETE FROM {metadata.TableName}";
            }
            
            // Fallback: use entity name as table name
            return $"DELETE FROM {entityName.ToLowerInvariant()}";
        }, RegexOptions.IgnoreCase);
        
        return cpql;
    }

    /// <summary>
    /// Converts CPQL INSERT INTO clause to SQL INSERT INTO clause
    /// Example: "INSERT INTO Product (Name, Price) VALUES (:name, :price)"
    /// becomes: "INSERT INTO products (name, price) VALUES (@name, @price)"
    /// </summary>
    private static string ConvertInsertClause(string cpql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap)
    {
        // Pattern: INSERT INTO EntityName (columns) VALUES (...)
        var insertPattern = @"\bINSERT\s+INTO\s+(\w+)\s*\(([^)]+)\)";
        cpql = Regex.Replace(cpql, insertPattern, match =>
        {
            var entityName = match.Groups[1].Value;
            var columnsList = match.Groups[2].Value;
            
            // Get table name from metadata
            string tableName;
            if (entitiesMetadata.TryGetValue(entityName, out var metadata))
            {
                tableName = metadata.TableName;
                
                // Convert column names using metadata
                var columns = columnsList.Split(',').Select(c => c.Trim()).ToList();
                var convertedColumns = new List<string>();
                
                foreach (var column in columns)
                {
                    var prop = metadata.Properties.FirstOrDefault(p => 
                        p.Name.Equals(column, StringComparison.OrdinalIgnoreCase));
                    
                    if (prop != null)
                    {
                        convertedColumns.Add(prop.ColumnName);
                    }
                    else
                    {
                        // Fallback: convert to snake_case
                        convertedColumns.Add(ToSnakeCase(column));
                    }
                }
                
                return $"INSERT INTO {tableName} ({string.Join(", ", convertedColumns)})";
            }
            
            // Fallback: convert entity name to lowercase and columns to snake_case
            tableName = entityName.ToLowerInvariant();
            var fallbackColumns = columnsList.Split(',')
                .Select(c => ToSnakeCase(c.Trim()));
            
            return $"INSERT INTO {tableName} ({string.Join(", ", fallbackColumns)})";
        }, RegexOptions.IgnoreCase);
        
        return cpql;
    }

    /// <summary>
    /// Extracts entity-to-alias mapping from CPQL query (e.g., {"Product": "p", "Category": "c"})
    /// </summary>
    private static Dictionary<string, string> ExtractEntityAliasMapping(string cpql)
    {
        var mapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        // Extract FROM clause: FROM EntityName alias
        var fromPattern = @"\bFROM\s+(\w+)\s+(\w+)";
        var fromMatch = Regex.Match(cpql, fromPattern, RegexOptions.IgnoreCase);
        if (fromMatch.Success)
        {
            var entityName = fromMatch.Groups[1].Value;
            var alias = fromMatch.Groups[2].Value;
            mapping[entityName] = alias;
        }
        
        // Extract UPDATE clause: UPDATE EntityName alias
        var updatePattern = @"\bUPDATE\s+(\w+)\s+(\w+)\s+SET";
        var updateMatch = Regex.Match(cpql, updatePattern, RegexOptions.IgnoreCase);
        if (updateMatch.Success)
        {
            var entityName = updateMatch.Groups[1].Value;
            var alias = updateMatch.Groups[2].Value;
            mapping[entityName] = alias;
        }
        
        // Extract JOIN clauses: JOIN EntityName alias
        var joinPattern = @"\b(?:INNER|LEFT|RIGHT|FULL)?\s*JOIN\s+(\w+)\s+(\w+)";
        var joinMatches = Regex.Matches(cpql, joinPattern, RegexOptions.IgnoreCase);
        foreach (Match match in joinMatches)
        {
            var entityName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            mapping[entityName] = alias;
        }
        
        return mapping;
    }

    /// <summary>
    /// Converts CPQL parameter syntax (:param) to SQL parameter syntax (@param)
    /// </summary>
    private static string ConvertParameterSyntax(string cpql)
    {
        // Replace :paramName with @paramName
        return Regex.Replace(cpql, @":(\w+)", "@$1");
    }

    /// <summary>
    /// Converts CPQL SELECT clause to SQL SELECT clause
    /// </summary>
    private static string ConvertSelectClause(string cpql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap)
    {
        // Pattern: SELECT [DISTINCT] alias [.property] or SELECT COUNT(alias) or SELECT AVG/SUM/MAX/MIN(alias.property)
        
        // Handle ALL aggregate functions in the query (not just in SELECT, but also in HAVING, etc.)
        // Pattern: COUNT(e), AVG(e.Property), SUM(e.Property), etc.
        var aggregatePattern = @"(COUNT|AVG|SUM|MAX|MIN)\s*\(\s*(\w+)(\.\w+)?\s*\)";
        cpql = Regex.Replace(cpql, aggregatePattern, match =>
        {
            var function = match.Groups[1].Value.ToUpper();
            var alias = match.Groups[2].Value;
            var property = match.Groups[3].Value; // Will be empty for COUNT(e), or .PropertyName
            
            if (string.IsNullOrEmpty(property))
            {
                // COUNT(e) -> COUNT(*)
                return $"{function}(*)";
            }
            else
            {
                // AVG(e.Property) -> AVG(column_name) - find entity by alias and get metadata
                var propName = property.TrimStart('.');
                var metadata = GetMetadataByAlias(alias, entitiesMetadata, entityAliasMap);
                var columnName = GetColumnName(propName, metadata);
                return $"{function}({columnName})";
            }
        }, RegexOptions.IgnoreCase);

        // Handle simple SELECT alias -> SELECT alias.col1 AS Prop1, alias.col2 AS Prop2, ...
        // But skip if it's:
        //  - a property list (contains comma or dot after alias)
        //  - an aggregate function name (COUNT, AVG, SUM, MAX, MIN)
        //  - followed by parenthesis (function call)
        var simpleSelectPattern = @"SELECT\s+(DISTINCT\s+)?(?!(COUNT|AVG|SUM|MAX|MIN)\b)([a-z]{1,3})\b(?!\.|,|\()";
        cpql = Regex.Replace(cpql, simpleSelectPattern, match =>
        {
            var distinct = match.Groups[1].Value; // Will be "DISTINCT " or empty
            var alias = match.Groups[3].Value;
            
            // Find metadata for this alias
            var metadata = GetMetadataByAlias(alias, entitiesMetadata, entityAliasMap);
            
            // If we have metadata, generate explicit column list with AS aliases for Dapper mapping
            // SELECT e.column_name AS PropertyName for each property
            if (metadata != null && metadata.Properties.Count > 0)
            {
                var columns = string.Join(", ", metadata.Properties.Select(p => 
                    $"{alias}.{p.ColumnName} AS {p.Name}"));
                return $"SELECT {distinct}{columns}";
            }
            
            // Fall back to SELECT * if no metadata
            return $"SELECT {distinct}*";
        }, RegexOptions.IgnoreCase);

        return cpql;
    }
    
    /// <summary>
    /// Helper to get metadata by alias using entity-alias mapping
    /// </summary>
    private static EntityMetadataInfo? GetMetadataByAlias(string alias, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap)
    {
        // Find entity name that has this alias
        var entityName = entityAliasMap.FirstOrDefault(kvp => 
            kvp.Value.Equals(alias, StringComparison.OrdinalIgnoreCase)).Key;
        
        if (entityName != null && entitiesMetadata.TryGetValue(entityName, out var metadata))
        {
            return metadata;
        }
        
        return null;
    }

    /// <summary>
    /// Converts CPQL FROM clause to SQL FROM clause
    /// </summary>
    private static string ConvertFromClause(string cpql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap)
    {
        // Pattern 1: FROM EntityName shortAlias (1-3 chars) -> FROM table_name alias (preserve alias when we have metadata)
        // Match "FROM EntityName alias" where alias is a short lowercase word (1-3 chars)
        // and is followed by end of string, WHERE, ORDER, GROUP, HAVING, LIMIT, or JOIN keywords
        var shortAliasPattern = @"\bFROM\s+(\w+)\s+([a-z]|[a-z][a-z0-9]{0,2})(?=\s*(?:WHERE|ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT|INNER|LEFT|RIGHT|JOIN|$))";
        cpql = Regex.Replace(cpql, shortAliasPattern, match =>
        {
            var entityName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            
            // Get table name from metadata if available
            var tableName = entitiesMetadata.TryGetValue(entityName, out var metadata) 
                ? metadata.TableName 
                : Pluralize(ToSnakeCase(entityName));
            
            // Preserve alias when we have metadata (for explicit column selection)
            if (metadata != null)
            {
                return $"FROM {tableName} {alias}";
            }
            return $"FROM {tableName}";
        }, RegexOptions.IgnoreCase);
        
        // Pattern 2: FROM EntityName longAlias (4+ chars) -> FROM table_name longAlias
        // This handles cases like "FROM Product product" where the alias is the lowercase entity name
        var longAliasPattern = @"\bFROM\s+(\w+)\s+(\w{4,})(?=\s*(?:WHERE|ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT|INNER|LEFT|RIGHT|JOIN|$))";
        cpql = Regex.Replace(cpql, longAliasPattern, match =>
        {
            var entityName = match.Groups[1].Value;
            var alias = match.Groups[2].Value;
            
            // Get table name from metadata if available
            var tableName = entitiesMetadata.TryGetValue(entityName, out var metadata) 
                ? metadata.TableName 
                : Pluralize(ToSnakeCase(entityName));
            return $"FROM {tableName} {alias}";
        }, RegexOptions.IgnoreCase);
        
        // Also handle JOIN clauses: JOIN EntityName alias -> JOIN table_name alias
        var joinPattern = @"\b(INNER\s+JOIN|LEFT\s+JOIN|RIGHT\s+JOIN|JOIN)\s+(\w+)\s+([a-z]|[a-z][a-z0-9]{0,2})(?=\s+ON)";
        cpql = Regex.Replace(cpql, joinPattern, match =>
        {
            var joinType = match.Groups[1].Value;
            var entityName = match.Groups[2].Value;
            var alias = match.Groups[3].Value;
            
            // Get table name from metadata if available
            var tableName = entitiesMetadata.TryGetValue(entityName, out var metadata) 
                ? metadata.TableName 
                : Pluralize(ToSnakeCase(entityName));
            return $"{joinType} {tableName} {alias}";
        }, RegexOptions.IgnoreCase);
        
        return cpql;
    }

    /// <summary>
    /// Removes entity aliases from property references and converts them to column names
    /// </summary>
    private static string RemoveEntityAliases(string cpql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap)
    {
        // Try to extract alias from the remaining query structure
        // Look for pattern: alias.PropertyName (e.g., p.Price, s.Name, cat.Id, product.Price, P.Price)
        
        // First, try to find alias from property references
        // Pattern matches: word.word (any length alias)
        var aliasFromPropertyPattern = @"\b(\w+)\.(\w+)";
        var matches = Regex.Matches(cpql, aliasFromPropertyPattern, RegexOptions.IgnoreCase);
        
        // Collect unique aliases (could be multiple in JOIN queries)
        var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in matches)
        {
            var potentialAlias = match.Groups[1].Value;
            // Skip if it looks like a database function or keyword
            // We identify aliases as short identifiers (typically 1-10 chars) that aren't SQL keywords
            if (potentialAlias.Length > 0 && potentialAlias.Length <= 10 &&
                !IsSqlKeyword(potentialAlias))
            {
                aliases.Add(potentialAlias);
            }
        }
        
        // Check if SELECT clause already has explicit column list with aliases (e.g., "SELECT s.id, s.email" or "SELECT DISTINCT s.id")
        // If so, only remove the MAIN entity's alias (the one we have metadata for)
        var hasExplicitSelectWithAliases = Regex.IsMatch(cpql, @"SELECT\s+(DISTINCT\s+)?\w+\.\w+", RegexOptions.IgnoreCase);
        
        // Remove each alias and convert property names to column names using metadata
        var result = cpql;
        
        if (hasExplicitSelectWithAliases && entitiesMetadata.Count > 0)
        {
            // Remove/convert ALL aliases using their respective metadata
            result = RemoveAliasesFromSpecificClauses(result, entitiesMetadata, entityAliasMap, new[] { "WHERE", "ORDER BY", "GROUP BY", "HAVING", "ON" });
        }
        else
        {
            // Original behavior: remove all aliases everywhere (for backward compatibility with queries without metadata)
            foreach (var alias in aliases)
            {
                result = Regex.Replace(result, $@"\b{Regex.Escape(alias)}\.(\w+)", match =>
                {
                    var propertyName = match.Groups[1].Value;
                    // Try to find metadata for this alias
                    var metadata = GetMetadataByAlias(alias, entitiesMetadata, entityAliasMap);
                    // Use metadata to get column name, or fall back to snake_case conversion
                    return GetColumnName(propertyName, metadata);
                }, RegexOptions.IgnoreCase);
            }
        }

        return result;
    }

    /// <summary>
    /// Removes aliases only from specific SQL clauses
    /// </summary>
    private static string RemoveAliasesFromSpecificClauses(string sql, Dictionary<string, EntityMetadataInfo> entitiesMetadata, Dictionary<string, string> entityAliasMap, string[] clauses)
    {
        var result = sql;
        
        foreach (var clause in clauses)
        {
            // Find the clause position
            var clausePattern = $@"\b{clause}\b";
            var clauseMatch = Regex.Match(result, clausePattern, RegexOptions.IgnoreCase);
            
            if (clauseMatch.Success)
            {
                var clauseStart = clauseMatch.Index;
                
                // Find the end of this clause (next major SQL keyword or end of string)
                // For ON clause, it ends at WHERE, ORDER BY, GROUP BY, HAVING, or end
                // For other clauses, it ends at next major keyword or end
                string nextClausePattern;
                if (clause.Equals("ON", StringComparison.OrdinalIgnoreCase))
                {
                    // ON clause ends at WHERE or other major clauses, but NOT at another JOIN
                    nextClausePattern = @"\b(WHERE|ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT|OFFSET)\b";
                }
                else
                {
                    nextClausePattern = @"\b(SELECT|FROM|WHERE|INNER|LEFT|RIGHT|JOIN|ORDER\s+BY|GROUP\s+BY|HAVING|LIMIT|OFFSET|UNION|EXCEPT|INTERSECT)\b";
                }
                
                var nextClauseMatch = Regex.Match(result.Substring(clauseStart + clause.Length), nextClausePattern, RegexOptions.IgnoreCase);
                
                int clauseEnd;
                if (nextClauseMatch.Success)
                {
                    clauseEnd = clauseStart + clause.Length + nextClauseMatch.Index;
                }
                else
                {
                    clauseEnd = result.Length;
                }
                
                // Extract the clause content
                var beforeClause = result.Substring(0, clauseStart);
                var clauseContent = result.Substring(clauseStart, clauseEnd - clauseStart);
                var afterClause = result.Substring(clauseEnd);
                
                // Remove all aliases in this clause using their respective metadata
                // Pattern: alias.Property
                clauseContent = Regex.Replace(clauseContent, @"\b(\w+)\.(\w+)", match =>
                {
                    var alias = match.Groups[1].Value;
                    var propertyName = match.Groups[2].Value;
                    
                    // Skip SQL keywords
                    if (IsSqlKeyword(alias))
                    {
                        return match.Value;
                    }
                    
                    // Try to find metadata for this alias
                    var metadata = GetMetadataByAlias(alias, entitiesMetadata, entityAliasMap);
                    return GetColumnName(propertyName, metadata);
                }, RegexOptions.IgnoreCase);
                
                result = beforeClause + clauseContent + afterClause;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Gets the column name for a property, using metadata if available or falling back to snake_case conversion
    /// </summary>
    private static string GetColumnName(string propertyName, EntityMetadataInfo? metadata)
    {
        if (metadata != null)
        {
            var prop = metadata.Properties.FirstOrDefault(p => 
                string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));
            
            if (prop != null && !string.IsNullOrEmpty(prop.ColumnName))
            {
                return prop.ColumnName;
            }
        }
        
        // Fall back to snake_case conversion
        return ToSnakeCase(propertyName);
    }
    
    /// <summary>
    /// Checks if a word is a SQL keyword that shouldn't be treated as an alias
    /// </summary>
    private static bool IsSqlKeyword(string word)
    {
        var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "JOIN", "INNER", "LEFT", "RIGHT", "OUTER",
            "ON", "AND", "OR", "NOT", "ORDER", "BY", "GROUP", "HAVING", "LIMIT",
            "OFFSET", "COUNT", "AVG", "SUM", "MAX", "MIN", "DISTINCT", "AS"
        };
        return keywords.Contains(word);
    }
    
    /// <summary>
    /// Converts PascalCase or camelCase to snake_case
    /// </summary>
    private static string ToSnakeCase(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Insert underscore before uppercase letters (except the first one)
        // and convert to lowercase
        var result = Regex.Replace(text, @"(?<!^)(?=[A-Z])", "_").ToLowerInvariant();
        return result;
    }
    
    /// <summary>
    /// Simple pluralization (adds 's' or 'es' as appropriate)
    /// </summary>
    private static string Pluralize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Handle common endings
        if (text.EndsWith("y", StringComparison.OrdinalIgnoreCase))
        {
            // category -> categories
            return text.Substring(0, text.Length - 1) + "ies";
        }
        else if (text.EndsWith("s", StringComparison.OrdinalIgnoreCase) ||
                 text.EndsWith("x", StringComparison.OrdinalIgnoreCase) ||
                 text.EndsWith("z", StringComparison.OrdinalIgnoreCase) ||
                 text.EndsWith("ch", StringComparison.OrdinalIgnoreCase) ||
                 text.EndsWith("sh", StringComparison.OrdinalIgnoreCase))
        {
            // class -> classes, box -> boxes
            return text + "es";
        }
        else
        {
            // Default: just add 's'
            return text + "s";
        }
    }
    
    /// <summary>
    /// Formats SQL for better readability by adding line breaks and indentation
    /// </summary>
    private static string FormatSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return sql;

        // Normalize whitespace first
        sql = Regex.Replace(sql, @"\s+", " ").Trim();

        // Add line breaks before major SQL clauses
        var keywords = new[] { "FROM", "WHERE", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL JOIN", 
                               "ORDER BY", "GROUP BY", "HAVING", "LIMIT", "OFFSET", "UNION" };
        
        foreach (var keyword in keywords)
        {
            // Use word boundary to match whole keywords only
            var pattern = $@"\b{Regex.Escape(keyword)}\b";
            sql = Regex.Replace(sql, pattern, $"\n{keyword}", RegexOptions.IgnoreCase);
        }

        // Special handling for AND/OR in WHERE clauses - add line breaks for readability
        // But only if they appear after WHERE
        var whereIndex = sql.IndexOf("\nWHERE", StringComparison.OrdinalIgnoreCase);
        if (whereIndex >= 0)
        {
            var orderByIndex = sql.IndexOf("\nORDER BY", StringComparison.OrdinalIgnoreCase);
            var groupByIndex = sql.IndexOf("\nGROUP BY", StringComparison.OrdinalIgnoreCase);
            var limitIndex = sql.IndexOf("\nLIMIT", StringComparison.OrdinalIgnoreCase);
            
            var whereEndIndex = sql.Length;
            if (orderByIndex > whereIndex) whereEndIndex = Math.Min(whereEndIndex, orderByIndex);
            if (groupByIndex > whereIndex) whereEndIndex = Math.Min(whereEndIndex, groupByIndex);
            if (limitIndex > whereIndex) whereEndIndex = Math.Min(whereEndIndex, limitIndex);
            
            if (whereEndIndex > whereIndex)
            {
                var whereClause = sql.Substring(whereIndex, whereEndIndex - whereIndex);
                // Add line breaks before AND/OR
                whereClause = Regex.Replace(whereClause, @"\s+(AND|OR)\s+", "\n  $1 ", RegexOptions.IgnoreCase);
                sql = sql.Substring(0, whereIndex) + whereClause + sql.Substring(whereEndIndex);
            }
        }

        // Ensure SELECT clause stays on one line if it's not too long
        // (unless it has multiple columns - then we keep the AS aliases together)
        
        return sql.Trim();
    }
}