using FluentAssertions;
using NPA.Providers.SqlServer;
using Xunit;

namespace NPA.Providers.SqlServer.Tests;

/// <summary>
/// Tests for SQL Server dialect implementation.
/// </summary>
public class SqlServerDialectTests
{
    private readonly SqlServerDialect _dialect;

    public SqlServerDialectTests()
    {
        _dialect = new SqlServerDialect();
    }

    [Theory]
    [InlineData("TableName", "[TableName]")]
    [InlineData("ColumnName", "[ColumnName]")]
    [InlineData("users", "[users]")]
    [InlineData("UserTable", "[UserTable]")]
    public void EscapeIdentifier_ShouldWrapWithSquareBrackets(string identifier, string expected)
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
    public void GetLastInsertedIdSql_ShouldReturnScopeIdentity()
    {
        // Act
        var sql = _dialect.GetLastInsertedIdSql();

        // Assert
        sql.Should().Be("SELECT SCOPE_IDENTITY()");
    }

    [Theory]
    [InlineData(0, 10, "SELECT * FROM users ORDER BY Id OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY")]
    [InlineData(10, 20, "SELECT * FROM users ORDER BY Id OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY")]
    [InlineData(100, 50, "SELECT * FROM users ORDER BY Id OFFSET 100 ROWS FETCH NEXT 50 ROWS ONLY")]
    public void GetPaginationSql_ShouldReturnOffsetFetch(int offset, int limit, string expected)
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
    public void GetDataTypeMapping_WithString_ShouldReturnNVARCHAR()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(string));

        // Assert
        result.Should().Be("NVARCHAR(255)");
    }

    [Fact]
    public void GetDataTypeMapping_WithDecimal_ShouldReturnDECIMAL()
    {
        // Act
        var result = _dialect.GetDataTypeMapping(typeof(decimal), precision: 10, scale: 2);

        // Assert
        result.Should().Be("DECIMAL(10,2)");
    }
}

