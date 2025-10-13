using NPA.Core.Query;
using NPA.Core.Query.CPQL;
using NPA.Core.Query.CPQL.AST;
using Xunit;

namespace NPA.Core.Tests.Query.CPQL;

/// <summary>
/// Tests for the enhanced CPQL Parser.
/// </summary>
public class ParserTests
{
    [Fact]
    public void Parse_SimpleSelectQuery_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT u FROM User u WHERE u.Username = :username";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Select, result.Type);
        Assert.Equal("User", result.EntityName);
        Assert.Equal("u", result.Alias);
        Assert.Contains("username", result.ParameterNames);
        Assert.NotNull(result.Ast);
    }
    
    [Fact]
    public void Parse_SelectWithOrderBy_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT u FROM User u ORDER BY u.CreatedAt DESC";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Select, result.Type);
        Assert.NotNull(result.Ast);
        
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.OrderByClause);
        Assert.Single(selectQuery.OrderByClause!.Items);
        Assert.Equal(OrderDirection.Descending, selectQuery.OrderByClause.Items[0].Direction);
    }
    
    [Fact]
    public void Parse_SelectWithJoin_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT u FROM User u INNER JOIN u.Orders o";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Select, result.Type);
        Assert.NotNull(result.Ast);
        
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.FromClause);
        Assert.Single(selectQuery.FromClause!.Joins);
        Assert.Equal(JoinType.Inner, selectQuery.FromClause.Joins[0].JoinType);
        Assert.Equal("Orders", selectQuery.FromClause.Joins[0].EntityName);
    }
    
    [Fact]
    public void Parse_SelectWithGroupBy_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT u.Department FROM User u GROUP BY u.Department";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Select, result.Type);
        Assert.NotNull(result.Ast);
        
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.GroupByClause);
        Assert.Single(selectQuery.GroupByClause!.Items);
    }
    
    [Fact]
    public void Parse_SelectWithHaving_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT u.Department, COUNT(u.Id) FROM User u GROUP BY u.Department HAVING COUNT(u.Id) > :minCount";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Select, result.Type);
        Assert.Contains("minCount", result.ParameterNames);
        Assert.NotNull(result.Ast);
        
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.HavingClause);
    }
    
    [Fact]
    public void Parse_SelectWithDistinct_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT DISTINCT u.Email FROM User u";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.True(selectQuery!.SelectClause?.IsDistinct);
    }
    
    [Fact]
    public void Parse_SelectWithAggregateFunction_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT COUNT(u.Id) FROM User u WHERE u.IsActive = :active";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Select, result.Type);
        Assert.Contains("active", result.ParameterNames);
        
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.SelectClause);
        Assert.Single(selectQuery.SelectClause!.Items);
        
        var firstItem = selectQuery.SelectClause.Items[0];
        Assert.IsType<AggregateExpression>(firstItem.Expression);
        
        var aggExpr = firstItem.Expression as AggregateExpression;
        Assert.Equal("COUNT", aggExpr!.FunctionName);
    }
    
    [Fact]
    public void Parse_SelectWithStringFunction_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT UPPER(u.Username) FROM User u";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.SelectClause);
        
        var firstItem = selectQuery.SelectClause!.Items[0];
        Assert.IsType<FunctionExpression>(firstItem.Expression);
        
        var funcExpr = firstItem.Expression as FunctionExpression;
        Assert.Equal("UPPER", funcExpr!.FunctionName);
    }
    
    [Fact]
    public void Parse_UpdateQuery_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "UPDATE User u SET u.IsActive = :active WHERE u.CreatedAt < :date";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Update, result.Type);
        Assert.Equal("User", result.EntityName);
        Assert.Equal("u", result.Alias);
        Assert.Contains("active", result.ParameterNames);
        Assert.Contains("date", result.ParameterNames);
        Assert.NotNull(result.Ast);
        
        var updateQuery = result.Ast as UpdateQuery;
        Assert.NotNull(updateQuery);
        Assert.Single(updateQuery!.Assignments);
        Assert.Equal("IsActive", updateQuery.Assignments[0].PropertyName);
    }
    
    [Fact]
    public void Parse_DeleteQuery_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "DELETE FROM User u WHERE u.IsActive = :active";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Equal(QueryType.Delete, result.Type);
        Assert.Equal("User", result.EntityName);
        Assert.Equal("u", result.Alias);
        Assert.Contains("active", result.ParameterNames);
        Assert.NotNull(result.Ast);
    }
    
    [Fact]
    public void Parse_ComplexWhereExpression_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT u FROM User u WHERE u.Age > :minAge AND u.Age < :maxAge OR u.IsAdmin = :isAdmin";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        Assert.Contains("minAge", result.ParameterNames);
        Assert.Contains("maxAge", result.ParameterNames);
        Assert.Contains("isAdmin", result.ParameterNames);
        
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.WhereClause);
        Assert.IsType<BinaryExpression>(selectQuery.WhereClause!.Condition);
    }
    
    [Fact]
    public void Parse_SelectWithMultipleJoins_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "SELECT u FROM User u INNER JOIN u.Orders o LEFT JOIN o.Product p";
        
        // Act
        var result = parser.Parse(cpql);
        
        // Assert
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.FromClause);
        Assert.Equal(2, selectQuery.FromClause!.Joins.Count);
        
        Assert.Equal(JoinType.Inner, selectQuery.FromClause.Joins[0].JoinType);
        Assert.Equal("Orders", selectQuery.FromClause.Joins[0].EntityName);
        
        Assert.Equal(JoinType.Left, selectQuery.FromClause.Joins[1].JoinType);
        Assert.Equal("Product", selectQuery.FromClause.Joins[1].EntityName);
    }

    [Fact]
    public void Parse_JoinWithKeywordAsEntityName_ShouldSucceed()
    {
        // Arrange
        var parser = new QueryParser();
        // "Order" is a keyword (OrderBy), so this tests if the parser can handle it as an identifier.
        var cpql = "SELECT c FROM Customer c JOIN c.Order o";

        // Act
        var result = parser.Parse(cpql);

        // Assert
        var selectQuery = result.Ast as SelectQuery;
        Assert.NotNull(selectQuery);
        Assert.NotNull(selectQuery!.FromClause);
        Assert.Single(selectQuery.FromClause!.Joins);
        Assert.Equal("Order", selectQuery.FromClause.Joins[0].EntityName);
    }
    
    [Fact]
    public void Parse_InvalidQuery_ShouldThrow()
    {
        // Arrange
        var parser = new QueryParser();
        var cpql = "INVALID QUERY SYNTAX";
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => parser.Parse(cpql));
    }
}
