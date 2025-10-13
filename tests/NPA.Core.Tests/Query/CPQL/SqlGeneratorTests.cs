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
    private readonly EntityMetadata _orderMetadata;
    private readonly EntityMetadata _userMetadata;

    public SqlGeneratorTests()
    {
        _metadataProvider = new MetadataProvider();
        _customerMetadata = _metadataProvider.GetEntityMetadata<TestCustomer>();
        _orderMetadata = _metadataProvider.GetEntityMetadata<TestOrder>();
        _userMetadata = _metadataProvider.GetEntityMetadata<TestUser>();
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

    // --- NEW TESTS FOR RELATIONSHIPS ---

    [Fact]
    public void Generate_JoinWithManyToOne_ShouldGenerateCorrectSql()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT o FROM TestOrder o JOIN o.Customer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _orderMetadata);
        
        // Assert
        Assert.Contains("FROM test_orders AS o", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INNER JOIN test_customers AS c ON o.customer_id = c.id", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_JoinWithOneToMany_ShouldGenerateCorrectSql()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT c FROM TestCustomer c JOIN c.Orders o";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _customerMetadata);
        
        // Assert
        Assert.Contains("FROM test_customers AS c", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INNER JOIN test_orders AS o ON c.id = o.customer_id", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_JoinWithSelectFromBothEntities_ShouldSelectAllColumns()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT o, c FROM TestOrder o JOIN o.Customer c";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _orderMetadata);
        
        // Assert
        // Check for columns from Order
        Assert.Contains("o.id AS Id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("o.order_date AS OrderDate", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("o.customer_id AS CustomerId", sql, StringComparison.OrdinalIgnoreCase);

        // Check for columns from Customer
        Assert.Contains("c.id AS Id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("c.name AS Name", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("c.email AS Email", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_JoinWithOneToOne_OwningSide_ShouldGenerateCorrectSql()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT p FROM TestUserProfile p JOIN p.User u";
        var userProfileMetadata = _metadataProvider.GetEntityMetadata<TestUserProfile>();

        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, userProfileMetadata);

        // Assert
        Assert.Contains("FROM test_user_profiles AS p", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INNER JOIN test_users AS u ON p.user_id = u.id", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Generate_JoinWithOneToOne_InverseSide_ShouldGenerateCorrectSql()
    {
        // Arrange
        var parser = new QueryParser();
        var sqlGenerator = new SqlGenerator(_metadataProvider, "SqlServer");
        var cpql = "SELECT u FROM TestUser u JOIN u.Profile p";
        
        // Act
        var parsedQuery = parser.Parse(cpql);
        var sql = sqlGenerator.Generate(parsedQuery, _userMetadata);

        // Assert
        Assert.Contains("FROM test_users AS u", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INNER JOIN test_user_profiles AS p ON u.id = p.user_id", sql, StringComparison.OrdinalIgnoreCase);
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

    [OneToMany(MappedBy = "Customer")]
    public ICollection<TestOrder> Orders { get; set; } = new List<TestOrder>();
}

[Entity]
[Table("test_orders")]
public class TestOrder
{
    [Id]
    [Column("id")]
    public long Id { get; set; }

    [Column("order_date")]
    public DateTime OrderDate { get; set; }

    [Column("customer_id")]
    public long CustomerId { get; set; }

    [ManyToOne]
    [JoinColumn("customer_id")]
    public TestCustomer Customer { get; set; } = null!;
}

[Entity]
[Table("test_users")]
public class TestUser
{
    [Id]
    [Column("id")]
    public long Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [OneToOne(MappedBy = "User")]
    public TestUserProfile Profile { get; set; } = null!;
}

[Entity]
[Table("test_user_profiles")]
public class TestUserProfile
{
    [Id]
    [Column("id")]
    public long Id { get; set; }

    [Column("bio")]
    public string Bio { get; set; } = string.Empty;

    [Column("user_id")]
    public long UserId { get; set; }

    [OneToOne]
    [JoinColumn("user_id")]
    public TestUser User { get; set; } = null!;
}
