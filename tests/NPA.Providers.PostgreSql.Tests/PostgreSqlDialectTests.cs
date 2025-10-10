using FluentAssertions;
using NPA.Providers.PostgreSql;
using Xunit;

namespace NPA.Providers.PostgreSql.Tests;

/// <summary>
/// Tests for PostgreSQL dialect implementation.
/// </summary>
public class PostgreSqlDialectTests
{
    private readonly PostgreSqlDialect _dialect;

    public PostgreSqlDialectTests()
    {
        _dialect = new PostgreSqlDialect();
    }

    [Theory]
    [InlineData("TableName", "\"TableName\"")]
    [InlineData("ColumnName", "\"ColumnName\"")]
    [InlineData("users", "\"users\"")]
    [InlineData("UserTable", "\"UserTable\"")]
    public void EscapeIdentifier_ShouldWrapWithDoubleQuotes(string identifier, string expected)
    {
        // Act
        var result = _dialect.EscapeIdentifier(identifier);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("table\"name", "\"table\"\"name\"")]
    [InlineData("col\"umn", "\"col\"\"umn\"")]
    public void EscapeIdentifier_WithQuotes_ShouldEscapeQuotes(string identifier, string expected)
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
    public void GetLastInsertedIdSql_ShouldReturnEmptyString()
    {
        // Act
        var sql = _dialect.GetLastInsertedIdSql();

        // Assert
        // PostgreSQL uses RETURNING clause instead of a separate query
        sql.Should().BeEmpty();
    }

    [Fact]
    public void GetNextSequenceValueSql_ShouldReturnNextvalFunction()
    {
        // Act
        var sql = _dialect.GetNextSequenceValueSql("user_id_seq");

        // Assert
        sql.Should().Contain("SELECT nextval");
        sql.Should().Contain("\"user_id_seq\"");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetNextSequenceValueSql_WithNullOrWhitespace_ShouldThrowException(string? sequenceName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetNextSequenceValueSql(sequenceName!));
    }

    [Theory]
    [InlineData(0, 10, "SELECT * FROM users LIMIT 10 OFFSET 0")]
    [InlineData(10, 20, "SELECT * FROM users LIMIT 20 OFFSET 10")]
    [InlineData(100, 50, "SELECT * FROM users LIMIT 50 OFFSET 100")]
    public void GetPaginationSql_ShouldReturnLimitOffset(int offset, int limit, string expected)
    {
        // Arrange
        var baseQuery = "SELECT * FROM users";

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

    [Theory]
    [InlineData(typeof(int), "INTEGER")]
    [InlineData(typeof(long), "BIGINT")]
    [InlineData(typeof(short), "SMALLINT")]
    [InlineData(typeof(byte), "SMALLINT")]
    [InlineData(typeof(bool), "BOOLEAN")]
    [InlineData(typeof(string), "TEXT")]
    [InlineData(typeof(DateTime), "TIMESTAMP")]
    [InlineData(typeof(Guid), "UUID")]
    [InlineData(typeof(decimal), "NUMERIC")]
    [InlineData(typeof(double), "DOUBLE PRECISION")]
    [InlineData(typeof(float), "REAL")]
    public void GetDataTypeMapping_WithBasicTypes_ShouldReturnCorrectMapping(Type type, string expectedType)
    {
        // Act
        var result = _dialect.GetDataTypeMapping(type);

        // Assert
        result.Should().Be(expectedType);
    }

    [Fact]
    public void GetDataTypeMapping_WithStringAndLength_ShouldReturnVarchar()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(string), length: 100);

        // Assert
        result.Should().Be("VARCHAR(100)");
    }

    [Fact]
    public void GetDataTypeMapping_WithDecimalAndPrecisionScale_ShouldReturnNumeric()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(decimal), precision: 18, scale: 2);

        // Assert
        result.Should().Be("NUMERIC(18,2)");
    }

    [Fact]
    public void GetDataTypeMapping_WithNullableType_ShouldReturnSameAsNonNullable()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(int?));

        // Assert
        result.Should().Be("INTEGER");
    }

    [Fact]
    public void GetDataTypeMapping_WithNullType_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _dialect.GetDataTypeMapping(null!));
    }

    [Fact]
    public void GetTableExistsSql_ShouldReturnInformationSchemaQuery()
    {
        // Act
        var sql = _dialect.GetTableExistsSql("users", "public");

        // Assert
        sql.Should().Contain("information_schema.tables");
        sql.Should().Contain("table_schema = @SchemaName");
        sql.Should().Contain("table_name = @TableName");
        sql.Should().Contain("EXISTS");
    }

    [Fact]
    public void GetTableExistsSql_WithNullSchema_ShouldUsePublicSchema()
    {
        // Act
        var sql = _dialect.GetTableExistsSql("users");

        // Assert
        sql.Should().Contain("information_schema.tables");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetTableExistsSql_WithNullOrWhitespaceTableName_ShouldThrowException(string? tableName)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetTableExistsSql(tableName!));
    }

    [Fact]
    public void GetCreateTableValuedParameterTypeSql_ShouldReturnCreateType()
    {
        // Arrange
        var columnDefinitions = new[] { "id INTEGER", "name TEXT" };

        // Act
        var sql = _dialect.GetCreateTableValuedParameterTypeSql("user_type", columnDefinitions);

        // Assert
        sql.Should().Contain("CREATE TYPE");
        sql.Should().Contain("\"user_type\"");
        sql.Should().Contain("id INTEGER");
        sql.Should().Contain("name TEXT");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCreateTableValuedParameterTypeSql_WithNullOrWhitespaceTypeName_ShouldThrowException(string? typeName)
    {
        // Arrange
        var columnDefinitions = new[] { "id INTEGER" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetCreateTableValuedParameterTypeSql(typeName!, columnDefinitions));
    }

    [Fact]
    public void GetFullTextSearchSql_ShouldReturnGinIndexCreation()
    {
        // Arrange
        var columnNames = new[] { "title", "description" };

        // Act
        var sql = _dialect.GetFullTextSearchSql("articles", columnNames);

        // Assert
        sql.Should().Contain("CREATE INDEX");
        sql.Should().Contain("USING GIN");
        sql.Should().Contain("to_tsvector");
        sql.Should().Contain("\"articles\"");
        sql.Should().Contain("\"title\"");
        sql.Should().Contain("\"description\"");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFullTextSearchSql_WithNullOrWhitespaceTableName_ShouldThrowException(string? tableName)
    {
        // Arrange
        var columnNames = new[] { "column1" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetFullTextSearchSql(tableName!, columnNames));
    }

    [Fact]
    public void GetFullTextSearchSql_WithNullOrEmptyColumns_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _dialect.GetFullTextSearchSql("table", (IEnumerable<string>)null!));
        Assert.Throws<ArgumentException>(() => _dialect.GetFullTextSearchSql("table", (IEnumerable<string>)Array.Empty<string>()));
    }
}

