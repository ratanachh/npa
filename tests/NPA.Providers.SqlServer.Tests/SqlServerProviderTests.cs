using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Providers.SqlServer;
using Xunit;

namespace NPA.Providers.SqlServer.Tests;

/// <summary>
/// Tests for SQL Server provider SQL generation.
/// </summary>
public class SqlServerProviderTests
{
    private readonly SqlServerProvider _provider;
    private readonly EntityMetadata _testEntityMetadata;

    public SqlServerProviderTests()
    {
        _provider = new SqlServerProvider();
        _testEntityMetadata = CreateTestEntityMetadata();
    }

    [Fact]
    public void GenerateInsertSql_WithIdentityColumn_ShouldIncludeScopeIdentity()
    {
        // Act
        var sql = _provider.GenerateInsertSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("INSERT INTO");
        sql.Should().Contain("[dbo].[users]");
        sql.Should().Contain("@Username");
        sql.Should().Contain("@Email");
        sql.Should().Contain("SELECT SCOPE_IDENTITY()");
        sql.Should().NotContain("[Id]"); // Identity column should not be in INSERT
    }

    [Fact]
    public void GenerateUpdateSql_ShouldIncludeAllNonPrimaryKeyColumns()
    {
        // Act
        var sql = _provider.GenerateUpdateSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("UPDATE [dbo].[users]");
        sql.Should().Contain("SET [Username] = @Username");
        sql.Should().Contain("[Email] = @Email");
        sql.Should().Contain("[CreatedAt] = @CreatedAt");
        sql.Should().Contain("[IsActive] = @IsActive");
        sql.Should().Contain("WHERE [Id] = @Id");
    }

    [Fact]
    public void GenerateDeleteSql_ShouldIncludeWhereClause()
    {
        // Act
        var sql = _provider.GenerateDeleteSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("DELETE FROM [dbo].[users]");
        sql.Should().Contain("WHERE [Id] = @id");
    }

    [Fact]
    public void GenerateSelectSql_ShouldIncludeAllColumns()
    {
        // Act
        var sql = _provider.GenerateSelectSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("SELECT [Id] AS Id, [Username] AS Username, [Email] AS Email, [CreatedAt] AS CreatedAt, [IsActive] AS IsActive");
        sql.Should().Contain("FROM [dbo].[users]");
    }

    [Fact]
    public void GenerateSelectByIdSql_ShouldIncludeWhereClause()
    {
        // Act
        var sql = _provider.GenerateSelectByIdSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("SELECT [Id] AS Id, [Username] AS Username, [Email] AS Email, [CreatedAt] AS CreatedAt, [IsActive] AS IsActive");
        sql.Should().Contain("FROM [dbo].[users]");
        sql.Should().Contain("WHERE [Id] = @id");
    }

    [Fact]
    public void GenerateCountSql_ShouldReturnCountQuery()
    {
        // Act
        var sql = _provider.GenerateCountSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Be("SELECT COUNT(*) FROM [dbo].[users];");
    }

    [Fact]
    public void ResolveTableName_WithSchema_ShouldIncludeSchema()
    {
        // Act
        var tableName = _provider.ResolveTableName(_testEntityMetadata);

        // Assert
        tableName.Should().Be("[dbo].[users]");
    }

    [Fact]
    public void ResolveTableName_WithoutSchema_ShouldNotIncludeSchema()
    {
        // Arrange
        var metadataWithoutSchema = CreateTestEntityMetadata(schemaName: null);

        // Act
        var tableName = _provider.ResolveTableName(metadataWithoutSchema);

        // Assert
        tableName.Should().Be("[users]");
    }

    [Fact]
    public void GetParameterPlaceholder_ShouldReturnAtSymbolPrefix()
    {
        // Act
        var placeholder = _provider.GetParameterPlaceholder("TestParam");

        // Assert
        placeholder.Should().Be("@TestParam");
    }

    [Fact]
    public void GetParameterPlaceholder_WithNullOrEmpty_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _provider.GetParameterPlaceholder(null!));
        Assert.Throws<ArgumentException>(() => _provider.GetParameterPlaceholder(""));
        Assert.Throws<ArgumentException>(() => _provider.GetParameterPlaceholder("   "));
    }

    [Fact]
    public void GenerateInsertSql_WithNullMetadata_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.GenerateInsertSql(null!));
    }

    [Fact]
    public void GenerateUpdateSql_WithNullMetadata_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.GenerateUpdateSql(null!));
    }

    [Fact]
    public void GenerateDeleteSql_WithNullMetadata_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.GenerateDeleteSql(null!));
    }

    [Fact]
    public void GenerateSelectSql_WithNullMetadata_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.GenerateSelectSql(null!));
    }

    [Fact]
    public void GenerateSelectByIdSql_WithNullMetadata_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.GenerateSelectByIdSql(null!));
    }

    [Fact]
    public void GenerateCountSql_WithNullMetadata_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.GenerateCountSql(null!));
    }

    [Fact]
    public void GenerateCreateTableSql_ShouldGenerateCompleteTableDefinition()
    {
        // Act
        var sql = _provider.GenerateCreateTableSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("CREATE TABLE [dbo].[users]");
        sql.Should().Contain("[Id] BIGINT IDENTITY(1,1) NOT NULL");
        sql.Should().Contain("[Username] NVARCHAR(50) NOT NULL");
        sql.Should().Contain("[Email] NVARCHAR(255) NOT NULL");
        sql.Should().Contain("PRIMARY KEY");
    }

    [Fact]
    public void GenerateSelectWithPaginationSql_ShouldIncludeOffsetFetch()
    {
        // Act
        var sql = _provider.GenerateSelectWithPaginationSql(_testEntityMetadata, "Id", 10, 20);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("ORDER BY [Id]");
        sql.Should().Contain("OFFSET 10 ROWS");
        sql.Should().Contain("FETCH NEXT 20 ROWS ONLY");
    }

    [Fact]
    public void GenerateSelectWithWhereSql_ShouldIncludeWhereConditions()
    {
        // Act
        var sql = _provider.GenerateSelectWithWhereSql(_testEntityMetadata, "[IsActive] = 1");

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("SELECT [Id] AS Id, [Username] AS Username, [Email] AS Email, [CreatedAt] AS CreatedAt, [IsActive] AS IsActive");
        sql.Should().Contain("FROM [dbo].[users]");
        sql.Should().Contain("WHERE [IsActive] = 1");
    }

    private EntityMetadata CreateTestEntityMetadata(string? schemaName = "dbo")
    {
        var metadata = new EntityMetadata
        {
            EntityType = typeof(TestUser),
            TableName = "users",
            SchemaName = schemaName
        };

        metadata.Properties.Add("Id", new PropertyMetadata
        {
            PropertyName = "Id",
            ColumnName = "Id",
            PropertyType = typeof(long),
            IsPrimaryKey = true,
            GenerationType = GenerationType.Identity,
            IsNullable = false
        });

        metadata.Properties.Add("Username", new PropertyMetadata
        {
            PropertyName = "Username",
            ColumnName = "Username",
            PropertyType = typeof(string),
            IsPrimaryKey = false,
            IsNullable = false,
            Length = 50
        });

        metadata.Properties.Add("Email", new PropertyMetadata
        {
            PropertyName = "Email",
            ColumnName = "Email",
            PropertyType = typeof(string),
            IsPrimaryKey = false,
            IsNullable = false,
            Length = 255,
            IsUnique = true
        });

        metadata.Properties.Add("CreatedAt", new PropertyMetadata
        {
            PropertyName = "CreatedAt",
            ColumnName = "CreatedAt",
            PropertyType = typeof(DateTime),
            IsPrimaryKey = false,
            IsNullable = false
        });

        metadata.Properties.Add("IsActive", new PropertyMetadata
        {
            PropertyName = "IsActive",
            ColumnName = "IsActive",
            PropertyType = typeof(bool),
            IsPrimaryKey = false,
            IsNullable = false
        });

        return metadata;
    }

    [Entity]
    [Table("users", Schema = "dbo")]
    private class TestUser
    {
        [Id]
        [GeneratedValue(GenerationType.Identity)]
        [Column("Id")]
        public long Id { get; set; }

        [Column("Username")]
        public string Username { get; set; } = string.Empty;

        [Column("Email")]
        public string Email { get; set; } = string.Empty;

        [Column("CreatedAt")]
        public DateTime CreatedAt { get; set; }

        [Column("IsActive")]
        public bool IsActive { get; set; }
    }
}

