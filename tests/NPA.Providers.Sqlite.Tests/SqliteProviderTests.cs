using FluentAssertions;
using NPA.Core.Annotations;
using NPA.Core.Metadata;
using NPA.Providers.Sqlite;
using Xunit;

namespace NPA.Providers.Sqlite.Tests;

/// <summary>
/// Tests for SQLite provider SQL generation.
/// </summary>
public class SqliteProviderTests
{
    private readonly SqliteProvider _provider;
    private readonly EntityMetadata _testEntityMetadata;

    public SqliteProviderTests()
    {
        _provider = new SqliteProvider();
        _testEntityMetadata = CreateTestEntityMetadata();
    }

    [Fact]
    public void GenerateInsertSql_WithAutoIncrementColumn_ShouldIncludeLastInsertRowId()
    {
        // Act
        var sql = _provider.GenerateInsertSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("INSERT INTO");
        sql.Should().Contain("\"users\"");
        sql.Should().Contain("@Username");
        sql.Should().Contain("@Email");
        sql.Should().Contain("SELECT last_insert_rowid()");
        sql.Should().NotContain("\"Id\""); // Auto increment column should not be in INSERT
    }

    [Fact]
    public void GenerateUpdateSql_ShouldIncludeAllNonPrimaryKeyColumns()
    {
        // Act
        var sql = _provider.GenerateUpdateSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("UPDATE \"users\"");
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
        sql.Should().Contain("DELETE FROM \"users\"");
        sql.Should().Contain("WHERE \"Id\" = @id");
    }

    [Fact]
    public void GenerateSelectSql_ShouldIncludeAllColumns()
    {
        // Act
        var sql = _provider.GenerateSelectSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("SELECT \"Id\" AS \"Id\", \"Username\" AS \"Username\", \"Email\" AS \"Email\", \"CreatedAt\" AS \"CreatedAt\", \"IsActive\" AS \"IsActive\"");
        sql.Should().Contain("FROM \"users\"");
    }

    [Fact]
    public void GenerateSelectByIdSql_ShouldIncludeWhereClause()
    {
        // Act
        var sql = _provider.GenerateSelectByIdSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Contain("SELECT \"Id\" AS \"Id\", \"Username\" AS \"Username\", \"Email\" AS \"Email\", \"CreatedAt\" AS \"CreatedAt\", \"IsActive\" AS \"IsActive\"");
        sql.Should().Contain("FROM \"users\"");
        sql.Should().Contain("WHERE \"Id\" = @id");
    }

    [Fact]
    public void GenerateCountSql_ShouldReturnCountQuery()
    {
        // Act
        var sql = _provider.GenerateCountSql(_testEntityMetadata);

        // Assert
        sql.Should().NotBeNullOrEmpty();
        sql.Should().Be("SELECT COUNT(*) FROM \"users\";");
    }

    [Fact]
    public void ResolveTableName_ShouldEscapeWithDoubleQuotes()
    {
        // Act
        var tableName = _provider.ResolveTableName(_testEntityMetadata);

        // Assert
        tableName.Should().Be("\"users\"");
    }

    [Fact]
    public void ResolveColumnName_ShouldEscapeWithDoubleQuotes()
    {
        // Arrange
        var property = _testEntityMetadata.Properties["Username"];

        // Act
        var columnName = _provider.ResolveColumnName(property);

        // Assert
        columnName.Should().Be("\"Username\"");
    }

    [Fact]
    public void GetParameterPlaceholder_ShouldReturnAtPrefix()
    {
        // Act
        var placeholder = _provider.GetParameterPlaceholder("Username");

        // Assert
        placeholder.Should().Be("@Username");
    }

    [Fact]
    public void ConvertParameterValue_WithDateTime_ShouldConvertToISO8601String()
    {
        // Arrange
        var dateTime = new DateTime(2024, 10, 10, 14, 30, 0);

        // Act
        var converted = _provider.ConvertParameterValue(dateTime, typeof(DateTime));

        // Assert
        converted.Should().BeOfType<string>();
        converted.Should().Be("2024-10-10 14:30:00.000");
    }

    [Fact]
    public void ConvertParameterValue_WithBoolean_ShouldConvertToInteger()
    {
        // Act
        var convertedTrue = _provider.ConvertParameterValue(true, typeof(bool));
        var convertedFalse = _provider.ConvertParameterValue(false, typeof(bool));

        // Assert
        convertedTrue.Should().Be(1L);
        convertedFalse.Should().Be(0L);
    }

    [Fact]
    public void Dialect_ShouldReturnSqliteDialect()
    {
        // Act
        var dialect = _provider.Dialect;

        // Assert
        dialect.Should().NotBeNull();
        dialect.Should().BeOfType<SqliteDialect>();
    }

    [Fact]
    public void TypeConverter_ShouldReturnSqliteTypeConverter()
    {
        // Act
        var typeConverter = _provider.TypeConverter;

        // Assert
        typeConverter.Should().NotBeNull();
        typeConverter.Should().BeOfType<SqliteTypeConverter>();
    }

    [Fact]
    public void BulkOperationProvider_ShouldReturnSqliteBulkOperationProvider()
    {
        // Act
        var bulkProvider = _provider.BulkOperationProvider;

        // Assert
        bulkProvider.Should().NotBeNull();
        bulkProvider.Should().BeOfType<SqliteBulkOperationProvider>();
    }

    private static EntityMetadata CreateTestEntityMetadata()
    {
        return new EntityMetadata
        {
            EntityType = typeof(TestUser),
            TableName = "users",
            SchemaName = null, // SQLite doesn't support schemas
            PrimaryKeyProperty = "Id",
            Properties = new Dictionary<string, PropertyMetadata>
            {
                ["Id"] = new PropertyMetadata
                {
                    PropertyName = "Id",
                    ColumnName = "Id",
                    PropertyType = typeof(long),
                    IsPrimaryKey = true,
                    IsNullable = false,
                    GenerationType = GenerationType.Identity
                },
                ["Username"] = new PropertyMetadata
                {
                    PropertyName = "Username",
                    ColumnName = "Username",
                    PropertyType = typeof(string),
                    IsPrimaryKey = false,
                    IsNullable = false
                },
                ["Email"] = new PropertyMetadata
                {
                    PropertyName = "Email",
                    ColumnName = "Email",
                    PropertyType = typeof(string),
                    IsPrimaryKey = false,
                    IsNullable = false
                },
                ["CreatedAt"] = new PropertyMetadata
                {
                    PropertyName = "CreatedAt",
                    ColumnName = "CreatedAt",
                    PropertyType = typeof(DateTime),
                    IsPrimaryKey = false,
                    IsNullable = false
                },
                ["IsActive"] = new PropertyMetadata
                {
                    PropertyName = "IsActive",
                    ColumnName = "IsActive",
                    PropertyType = typeof(bool),
                    IsPrimaryKey = false,
                    IsNullable = false
                }
            }
        };
    }
}

[Entity]
[Table("users")]
internal class TestUser
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

