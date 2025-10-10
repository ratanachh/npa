using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Core.Query;
using NPA.Core.Query.CPQL;
using Xunit;

namespace NPA.Core.Tests.Query.CPQL;

/// <summary>
/// Tests for SqlGenerator with enhanced CPQL features.
/// </summary>
public class SqlGeneratorTests
{
    private readonly IMetadataProvider _metadataProvider;
    private readonly EntityMetadata _customerMetadata;

    public SqlGeneratorTests()
    {
        _metadataProvider = new MetadataProvider();
        _customerMetadata = _metadataProvider.GetEntityMetadata<TestCustomer>();
    }

    [Fact]
    public void Generate_SelectWithAliasOnly_ShouldGenerateAllColumns()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT c FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("c.id AS Id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("c.name AS Name", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("c.email AS Email", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM test_customers AS c", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Invalid column name", sql);
    }

    [Fact]
    public void Generate_CountWithAliasOnly_ShouldUsePrimaryKey()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT COUNT(c) FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("COUNT(c.id)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM test_customers AS c", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COUNT(c)", sql); // Should be COUNT(c.id) not COUNT(c)
    }

    [Fact]
    public void Generate_CountWithSpecificProperty_ShouldUseProperty()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT COUNT(c.Email) FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("COUNT(c.email)", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COUNT(c.id)", sql);
    }

    [Fact]
    public void Generate_CountDistinctWithAlias_ShouldUsePrimaryKey()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT COUNT(DISTINCT c) FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("COUNT(DISTINCT c.id)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_SelectWithWhereAndAlias_ShouldGenerateCorrectly()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT c FROM TestCustomer c WHERE c.IsActive = :active";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("SELECT c.id AS Id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM test_customers AS c", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE (c.is_active = @active)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_SumWithAlias_ShouldUsePrimaryKey()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT SUM(c) FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert - SUM(entity) should use primary key (though semantically odd)
        Assert.Contains("SUM(c.id)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_AvgWithSpecificProperty_ShouldUseProperty()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT AVG(c.Id) FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("AVG(c.id)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_SelectSpecificProperty_ShouldGenerateOnlyThatColumn()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT c.Email FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("c.email", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("c.name", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("c.id AS Id", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_UpdateQuery_ShouldGenerateCorrectly()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "UPDATE TestCustomer c SET c.IsActive = :active WHERE c.Id = :id";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("UPDATE test_customers SET", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("is_active = @active", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE (id = @id)", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_DeleteQuery_ShouldGenerateCorrectly()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "DELETE FROM TestCustomer c WHERE c.IsActive = :active";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("DELETE FROM test_customers", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE (c.is_active = @active)", sql, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void Generate_SqlServerDialect_ShouldNotQuoteIdentifiers()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT c FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert - SQL Server should not use quotes for simple identifiers
        Assert.Contains("c.id AS Id", sql);
        Assert.Contains("c.name AS Name", sql);
        Assert.DoesNotContain("\"Id\"", sql);
        Assert.DoesNotContain("`Id`", sql);
    }
    
    [Fact]
    public void Generate_PostgreSqlDialect_ShouldUseDoubleQuotes()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "PostgreSQL");
        var cpql = "SELECT c FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert - PostgreSQL should use double quotes for case sensitivity
        Assert.Contains("c.id AS \"Id\"", sql);
        Assert.Contains("c.name AS \"Name\"", sql);
        Assert.Contains("c.email AS \"Email\"", sql);
    }
    
    [Fact]
    public void Generate_MySqlDialect_ShouldUseBackticks()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "MySQL");
        var cpql = "SELECT c FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert - MySQL should use backticks
        Assert.Contains("c.id AS `Id`", sql);
        Assert.Contains("c.name AS `Name`", sql);
        Assert.Contains("c.email AS `Email`", sql);
    }
    
    [Fact]
    public void Generate_MariaDbDialect_ShouldUseBackticks()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "MariaDB");
        var cpql = "SELECT c FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert - MariaDB should use backticks (same as MySQL)
        Assert.Contains("c.id AS `Id`", sql);
        Assert.Contains("c.name AS `Name`", sql);
        Assert.Contains("c.email AS `Email`", sql);
    }
    
    [Fact]
    public void Generate_SqliteDialect_ShouldUseDoubleQuotes()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SQLite");
        var cpql = "SELECT c FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert - SQLite should use double quotes (SQL standard)
        Assert.Contains("c.id AS \"Id\"", sql);
        Assert.Contains("c.name AS \"Name\"", sql);
        Assert.Contains("c.email AS \"Email\"", sql);
    }
    
    [Fact]
    public void Generate_DefaultDialect_ShouldNotQuoteIdentifiers()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "default");
        var cpql = "SELECT c FROM TestCustomer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert - Default should not use quotes
        Assert.Contains("c.id AS Id", sql);
        Assert.Contains("c.name AS Name", sql);
        Assert.DoesNotContain("\"Id\"", sql);
        Assert.DoesNotContain("`Id`", sql);
    }
}

/// <summary>
/// Test entity for SQL generation tests.
/// </summary>
[Entity]
[Table("test_customers")]
public class TestCustomer
{
    [Id]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; }
}

