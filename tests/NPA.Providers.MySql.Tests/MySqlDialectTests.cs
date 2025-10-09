using FluentAssertions;
using NPA.Providers.MySql;
using Xunit;

namespace NPA.Providers.MySql.Tests;

/// <summary>
/// Tests for MySQL dialect implementation.
/// </summary>
public class MySqlDialectTests
{
    private readonly MySqlDialect _dialect;

    public MySqlDialectTests()
    {
        _dialect = new MySqlDialect();
    }

    [Theory]
    [InlineData("TableName", "`TableName`")]
    [InlineData("ColumnName", "`ColumnName`")]
    [InlineData("users", "`users`")]
    [InlineData("UserTable", "`UserTable`")]
    public void EscapeIdentifier_ShouldWrapWithBackticks(string identifier, string expected)
    {
        // Act
        var result = _dialect.EscapeIdentifier(identifier);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EscapeIdentifier_WithNullOrWhitespace_ShouldThrowException(string? identifier)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.EscapeIdentifier(identifier!));
    }

    [Fact]
    public void GetLastInsertedIdSql_ShouldReturnLastInsertId()
    {
        // Act
        var sql = _dialect.GetLastInsertedIdSql();

        // Assert
        sql.Should().Be("SELECT LAST_INSERT_ID()");
    }

    [Theory]
    [InlineData(0, 10, "SELECT * FROM users ORDER BY Id LIMIT 0, 10")]
    [InlineData(10, 20, "SELECT * FROM users ORDER BY Id LIMIT 10, 20")]
    [InlineData(100, 50, "SELECT * FROM users ORDER BY Id LIMIT 100, 50")]
    public void GetPaginationSql_ShouldReturnLimitClause(int offset, int limit, string expected)
    {
        // Arrange
        var baseQuery = "SELECT * FROM users ORDER BY Id";

        // Act
        var result = _dialect.GetPaginationSql(baseQuery, offset, limit);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetPaginationSql_WithNegativeOffset_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetPaginationSql("SELECT * FROM users", -1, 10));
    }

    [Fact]
    public void GetPaginationSql_WithNegativeLimit_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetPaginationSql("SELECT * FROM users", 0, -1));
    }

    [Fact]
    public void GetPaginationSql_WithZeroLimit_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetPaginationSql("SELECT * FROM users", 0, 0));
    }

    [Fact]
    public void GetPaginationSql_WithNullBaseQuery_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetPaginationSql(null!, 0, 10));
    }

    [Fact]
    public void GetDataTypeMapping_WithInt_ShouldReturnINT()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(int));

        // Assert
        result.Should().Be("INT");
    }

    [Fact]
    public void GetDataTypeMapping_WithString_ShouldReturnVARCHAR()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(string));

        // Assert
        result.Should().Be("VARCHAR(255)");
    }

    [Fact]
    public void GetDataTypeMapping_WithStringAndLength_ShouldReturnVARCHARWithLength()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(string), length: 50);

        // Assert
        result.Should().Be("VARCHAR(50)");
    }

    [Fact]
    public void GetDataTypeMapping_WithDecimal_ShouldReturnDECIMAL()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(decimal), precision: 10, scale: 2);

        // Assert
        result.Should().Be("DECIMAL(10,2)");
    }

    [Fact]
    public void GetDataTypeMapping_WithBool_ShouldReturnTINYINT()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(bool));

        // Assert
        result.Should().Be("TINYINT(1)");
    }

    [Fact]
    public void GetDataTypeMapping_WithDateTime_ShouldReturnDATETIME()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(DateTime));

        // Assert
        result.Should().Be("DATETIME");
    }

    [Fact]
    public void GetDataTypeMapping_WithGuid_ShouldReturnCHAR36()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(Guid));

        // Assert
        result.Should().Be("CHAR(36)");
    }

    [Fact]
    public void GetMatchAgainstSql_ShouldGenerateFullTextSearch()
    {
        // Arrange
        var columns = new[] { "title", "description" };

        // Act
        var result = _dialect.GetMatchAgainstSql(columns);

        // Assert
        result.Should().Contain("MATCH(`title`, `description`)");
        result.Should().Contain("AGAINST(@searchTerm IN NATURAL LANGUAGE MODE)");
    }

    [Fact]
    public void GetJsonExtractSql_ShouldGenerateJsonExtract()
    {
        // Act
        var result = _dialect.GetJsonExtractSql("metadata", "$.price");

        // Assert
        result.Should().Be("JSON_EXTRACT(`metadata`, '$.price')");
    }

    [Fact]
    public void GetJsonValidSql_ShouldGenerateJsonValid()
    {
        // Act
        var result = _dialect.GetJsonValidSql("data");

        // Assert
        result.Should().Be("JSON_VALID(`data`)");
    }

    [Fact]
    public void GetAutoIncrementColumnSql_ShouldGenerateAutoIncrement()
    {
        // Act
        var result = _dialect.GetAutoIncrementColumnSql("id", "BIGINT");

        // Assert
        result.Should().Be("`id` BIGINT AUTO_INCREMENT NOT NULL");
    }

    [Fact]
    public void GetSpatialIndexSql_ShouldGenerateSpatialIndex()
    {
        // Act
        var result = _dialect.GetSpatialIndexSql("locations", "coordinates");

        // Assert
        result.Should().Contain("CREATE SPATIAL INDEX");
        result.Should().Contain("`SP_IDX_locations_coordinates`");
        result.Should().Contain("ON `locations`");
        result.Should().Contain("(`coordinates`)");
    }

    [Fact]
    public void GetNextSequenceValueSql_ShouldReturnNextVal()
    {
        // Act
        var result = _dialect.GetNextSequenceValueSql("user_seq");

        // Assert
        result.Should().Contain("SELECT NEXTVAL(`user_seq`)");
    }

    [Fact]
    public void GetTableExistsSql_WithSchema_ShouldCheckWithSchema()
    {
        // Act
        var result = _dialect.GetTableExistsSql("users", "mydb");

        // Assert
        result.Should().Contain("INFORMATION_SCHEMA.TABLES");
        result.Should().Contain("TABLE_SCHEMA = @SchemaName");
        result.Should().Contain("TABLE_NAME = @TableName");
    }

    [Fact]
    public void GetTableExistsSql_WithoutSchema_ShouldUseDatabase()
    {
        // Act
        var result = _dialect.GetTableExistsSql("users", null);

        // Assert
        result.Should().Contain("INFORMATION_SCHEMA.TABLES");
        result.Should().Contain("TABLE_SCHEMA = DATABASE()");
        result.Should().Contain("TABLE_NAME = @TableName");
    }

    [Fact]
    public void GetCreateTableValuedParameterTypeSql_ShouldThrowNotSupported()
    {
        // Act & Assert
        Assert.Throws<NotSupportedException>(() => 
            _dialect.GetCreateTableValuedParameterTypeSql("TestType", new[] { "col1 INT" }));
    }
}

