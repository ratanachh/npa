using System.Text.RegularExpressions;

namespace NPA.Core.Query;

/// <summary>
/// Parses CPQL-like queries into structured representations.
/// </summary>
public class QueryParser : IQueryParser
{
    private static readonly Regex SelectPattern = new(@"SELECT\s+(\w+)\s+FROM\s+(\w+)\s+(\w+)(?:\s+WHERE\s+(.+?))?(?:\s+ORDER\s+BY\s+(.+?))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SelectCountPattern = new(@"SELECT\s+COUNT\((\w+(?:\.\w+)?)\)\s+FROM\s+(\w+)\s+(\w+)(?:\s+WHERE\s+(.+?))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UpdatePattern = new(@"UPDATE\s+(\w+)\s+(\w+)\s+SET\s+(.+?)(?:\s+WHERE\s+(.+?))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeletePattern = new(@"DELETE\s+FROM\s+(\w+)\s+(\w+)(?:\s+WHERE\s+(.+?))?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ParameterPattern = new(@":(\w+)", RegexOptions.Compiled);

    /// <inheritdoc />
    public ParsedQuery Parse(string cpql)
    {
        if (string.IsNullOrWhiteSpace(cpql))
            throw new ArgumentException("CPQL query cannot be null or empty", nameof(cpql));

        var trimmedCpql = cpql.Trim();

        // Try to match SELECT COUNT query
        var selectCountMatch = SelectCountPattern.Match(trimmedCpql);
        if (selectCountMatch.Success)
        {
            var query = ParseSelectCountQuery(selectCountMatch);
            query.OriginalCpql = cpql;
            return query;
        }

        // Try to match SELECT query
        var selectMatch = SelectPattern.Match(trimmedCpql);
        if (selectMatch.Success)
        {
            var query = ParseSelectQuery(selectMatch);
            query.OriginalCpql = cpql;
            return query;
        }

        // Try to match UPDATE query
        var updateMatch = UpdatePattern.Match(trimmedCpql);
        if (updateMatch.Success)
        {
            var query = ParseUpdateQuery(updateMatch);
            query.OriginalCpql = cpql;
            return query;
        }

        // Try to match DELETE query
        var deleteMatch = DeletePattern.Match(trimmedCpql);
        if (deleteMatch.Success)
        {
            var query = ParseDeleteQuery(deleteMatch);
            query.OriginalCpql = cpql;
            return query;
        }

        throw new ArgumentException($"Invalid CPQL syntax: {cpql}", nameof(cpql));
    }

    private ParsedQuery ParseSelectCountQuery(Match match)
    {
        var query = new ParsedQuery
        {
            Type = QueryType.Select,
            EntityName = match.Groups[2].Value,
            Alias = match.Groups[3].Value
        };

        if (match.Groups[4].Success)
        {
            query.WhereClause = match.Groups[4].Value.Trim();
        }

        ExtractParameters(query);
        return query;
    }

    private ParsedQuery ParseSelectQuery(Match match)
    {
        var query = new ParsedQuery
        {
            Type = QueryType.Select,
            EntityName = match.Groups[2].Value,
            Alias = match.Groups[3].Value
        };

        if (match.Groups[4].Success)
        {
            query.WhereClause = match.Groups[4].Value.Trim();
        }

        if (match.Groups[5].Success)
        {
            query.OrderByClause = match.Groups[5].Value.Trim();
        }

        ExtractParameters(query);
        return query;
    }

    private ParsedQuery ParseUpdateQuery(Match match)
    {
        var query = new ParsedQuery
        {
            Type = QueryType.Update,
            EntityName = match.Groups[1].Value,
            Alias = match.Groups[2].Value,
            SetClause = match.Groups[3].Value.Trim()
        };

        if (match.Groups[4].Success)
        {
            query.WhereClause = match.Groups[4].Value.Trim();
        }

        ExtractParameters(query);
        return query;
    }

    private ParsedQuery ParseDeleteQuery(Match match)
    {
        var query = new ParsedQuery
        {
            Type = QueryType.Delete,
            EntityName = match.Groups[1].Value,
            Alias = match.Groups[2].Value
        };

        if (match.Groups[3].Success)
        {
            query.WhereClause = match.Groups[3].Value.Trim();
        }

        ExtractParameters(query);
        return query;
    }

    private void ExtractParameters(ParsedQuery query)
    {
        var parameterNames = new List<string>();

        // Extract parameters from WHERE clause
        if (!string.IsNullOrEmpty(query.WhereClause))
        {
            var whereMatches = ParameterPattern.Matches(query.WhereClause);
            foreach (Match match in whereMatches)
            {
                parameterNames.Add(match.Groups[1].Value);
            }
        }

        // Extract parameters from SET clause
        if (!string.IsNullOrEmpty(query.SetClause))
        {
            var setMatches = ParameterPattern.Matches(query.SetClause);
            foreach (Match match in setMatches)
            {
                parameterNames.Add(match.Groups[1].Value);
            }
        }

        query.ParameterNames = parameterNames.Distinct().ToList();
    }
}
