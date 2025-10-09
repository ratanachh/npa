using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Providers.PostgreSql;
using Xunit;

namespace NPA.Providers.PostgreSql.Tests;

/// <summary>
/// Tests for PostgreSQL provider SQL generation.
/// </summary>
public class PostgreSqlProviderTests
{
    private readonly PostgreSqlProvider _provider;
    private readonly EntityMetadata _testEntityMetadata;

    public PostgreSqlProviderTests()
    {
        _provider = new PostgreSqlProvider();
        _testEntityMetadata = CreateTestEntityMetadata();
    }

    [Fact]
    public void GenerateInsertSql_WithIdentityColumn_ShouldIncludeReturningClause()
    {
        // Act
        var sql = _provider.GenerateInsertSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("INSERT INTO");
        sql.Should().Contain("\"public\".\"users\"");
        sql.Should().Contain("@Username");
        sql.Should().Contain("@Email");
        sql.Should().Contain("RETURNING \"Id\"");
        // Verify Id is not in the column list before VALUES (only in RETURNING)
        var beforeValues = sql.Substring(0, sql.IndexOf("VALUES"));
        beforeValues.Should().NotContain("\"Id\"");
    }

    [Fact]
    public void GenerateUpdateSql_ShouldIncludeAllNonPrimaryKeyColumns()
    {
        // Act
        var sql = _provider.GenerateUpdateSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("UPDATE \"public\".\"users\"");
        sql.Should().Contain("SET \"Username\" = @Username");
        sql.Should().Contain("\"Email\" = @Email");
        sql.Should().Contain("\"CreatedAt\" = @CreatedAt");
        sql.Should().Contain("\"IsActive\" = @IsActive");
        sql.Should().Contain("WHERE \"Id\" = @Id");
    }

    [Fact]
    public void GenerateDeleteSql_ShouldIncludeWhereClause()
    {
        // Act
        var sql = _provider.GenerateDeleteSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("DELETE FROM \"public\".\"users\"");
        sql.Should().Contain("WHERE \"Id\" = @id");
    }

    [Fact]
    public void GenerateSelectSql_ShouldIncludeAllColumns()
    {
        // Act
        var sql = _provider.GenerateSelectSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("SELECT \"Id\", \"Username\", \"Email\", \"CreatedAt\", \"IsActive\"");
        sql.Should().Contain("FROM \"public\".\"users\"");
    }

    [Fact]
    public void GenerateSelectByIdSql_ShouldIncludeWhereClause()
    {
        // Act
        var sql = _provider.GenerateSelectByIdSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("SELECT \"Id\", \"Username\", \"Email\", \"CreatedAt\", \"IsActive\"");
        sql.Should().Contain("FROM \"public\".\"users\"");
        sql.Should().Contain("WHERE \"Id\" = @id");
    }

    [Fact]
    public void GenerateCountSql_ShouldReturnCountQuery()
    {
        // Act
        var sql = _provider.GenerateCountSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Be("SELECT COUNT(*) FROM \"public\".\"users\";");
    }

    [Fact]
    public void ResolveTableName_WithSchema_ShouldIncludeSchema()
    {
        // Act
        var tableName = _provider.ResolveTableName(_testEntityMetadata);

        // Assert
        tableName.Should().Be("\"public\".\"users\"");
    }

    [Fact]
    public void ResolveTableName_WithoutSchema_ShouldNotIncludeSchema()
    {
        // Arrange
        var metadataWithoutSchema = CreateTestEntityMetadata(schemaName: null);

        // Act
        var tableName = _provider.ResolveTableName(metadataWithoutSchema);

        // Assert
        tableName.Should().Be("\"users\"");
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
    public void ConvertParameterValue_WithBoolTrue_ShouldReturnTrue()
    {
        // Act
        var result = _provider.ConvertParameterValue(true, typeof(bool));

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertParameterValue_WithBoolFalse_ShouldReturnFalse()
    {
        // Act
        var result = _provider.ConvertParameterValue(false, typeof(bool));

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertParameterValue_WithNull_ShouldReturnNull()
    {
        // Act
        var result = _provider.ConvertParameterValue(null, typeof(string));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertParameterValue_WithString_ShouldReturnSameValue()
    {
        // Arrange
        string value = "test";

        // Act
        var result = _provider.ConvertParameterValue(value, typeof(string));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertParameterValue_WithInt_ShouldReturnSameValue()
    {
        // Arrange
        int value = 42;

        // Act
        var result = _provider.ConvertParameterValue(value, typeof(int));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void GenerateInsertSql_WithoutIdentityColumn_ShouldNotIncludeReturning()
    {
        // Arrange
        var metadata = CreateTestEntityMetadata();
        metadata.Properties["Id"].GenerationType = GenerationType.None;

        // Act
        var sql = _provider.GenerateInsertSql(metadata);

        // Assert
        sql.Should().NotContain("RETURNING");
    }

    [Fact]
    public void GenerateUpdateSql_WithSingleColumn_ShouldGenerateValidSql()
    {
        // Arrange
        var metadata = new EntityMetadata
        {
            EntityType = typeof(SimpleEntity),
            TableName = "simple",
            SchemaName = "public"
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

        metadata.Properties.Add("Name", new PropertyMetadata
        {
            PropertyName = "Name",
            ColumnName = "Name",
            PropertyType = typeof(string),
            IsPrimaryKey = false,
            IsNullable = false
        });

        // Act
        var sql = _provider.GenerateUpdateSql(metadata);

        // Assert
        sql.Should().Contain("UPDATE \"public\".\"simple\"");
        sql.Should().Contain("SET \"Name\" = @Name");
        sql.Should().Contain("WHERE \"Id\" = @Id");
    }

    [Fact]
    public void ResolveColumnName_ShouldWrapWithDoubleQuotes()
    {
        // Arrange
        var property = new PropertyMetadata
        {
            PropertyName = "TestProperty",
            ColumnName = "test_column",
            PropertyType = typeof(string)
        };

        // Act
        var result = _provider.ResolveColumnName(property);

        // Assert
        result.Should().Be("\"test_column\"");
    }

    [Fact]
    public void ResolveColumnName_WithNullProperty_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.ResolveColumnName(null!));
    }

    [Fact]
    public void ResolveTableName_WithNullMetadata_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _provider.ResolveTableName(null!));
    }

    private EntityMetadata CreateTestEntityMetadata(string? schemaName = "public")
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
    [Table("users", Schema = "public")]
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

    private class SimpleEntity
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}

