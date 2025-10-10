using FluentAssertions;
using NPA.Providers.Sqlite;
using Xunit;

namespace NPA.Providers.Sqlite.Tests;

/// <summary>
/// Tests for SQLite dialect.
/// </summary>
public class SqliteDialectTests
{
    private readonly SqliteDialect _dialect;

    public SqliteDialectTests()
    {
        _dialect = new SqliteDialect();
    }

    [Fact]
    public void GetLastInsertedIdSql_ShouldReturnLastInsertRowId()
    {
        // Act
        var sql = _dialect.GetLastInsertedIdSql();

        // Assert
        sql.Should().Be("SELECT last_insert_rowid()");
    }

    [Fact]
    public void EscapeIdentifier_ShouldUseDoubleQuotes()
    {
        // Act
        var escaped = _dialect.EscapeIdentifier("users");

        // Assert
        escaped.Should().Be("\"users\"");
    }

    [Fact]
    public void EscapeIdentifier_WithQuotes_ShouldEscapeQuotes()
    {
        // Act
        var escaped = _dialect.EscapeIdentifier("table\"name");

        // Assert
        escaped.Should().Be("\"table\"\"name\"");
    }

    [Fact]
    public void GetTableExistsSql_ShouldUseSqliteMaster()
    {
        // Act
        var sql = _dialect.GetTableExistsSql("users");

        // Assert
        sql.Should().Contain("sqlite_master");
        sql.Should().Contain("type='table'");
        sql.Should().Contain("@TableName");
    }

    [Fact]
    public void GetPaginationSql_ShouldUseLimitOffset()
    {
        // Arrange
        var baseSql = "SELECT * FROM users";

        // Act
        var sql = _dialect.GetPaginationSql(baseSql, 10, 20);

        // Assert
        sql.Should().Be("SELECT * FROM users LIMIT 20 OFFSET 10");
    }

    [Fact]
    public void GetDataTypeMapping_WithInt_ShouldReturnInteger()
    {
        // Act
        var type = _dialect.GetDataTypeMapping(typeof(int));

        // Assert
        type.Should().Be("INTEGER");
    }

    [Fact]
    public void GetDataTypeMapping_WithBool_ShouldReturnInteger()
    {
        // Act
        var type = _dialect.GetDataTypeMapping(typeof(bool));

        // Assert
        type.Should().Be("INTEGER");
    }

    [Fact]
    public void GetDataTypeMapping_WithString_ShouldReturnText()
    {
        // Act
        var type = _dialect.GetDataTypeMapping(typeof(string));

        // Assert
        type.Should().Be("TEXT");
    }

    [Fact]
    public void GetDataTypeMapping_WithDateTime_ShouldReturnText()
    {
        // Act
        var type = _dialect.GetDataTypeMapping(typeof(DateTime));

        // Assert
        type.Should().Be("TEXT");
    }

    [Fact]
    public void GetDataTypeMapping_WithDouble_ShouldReturnReal()
    {
        // Act
        var type = _dialect.GetDataTypeMapping(typeof(double));

        // Assert
        type.Should().Be("REAL");
    }

    [Fact]
    public void GetDataTypeMapping_WithByteArray_ShouldReturnBlob()
    {
        // Act
        var type = _dialect.GetDataTypeMapping(typeof(byte[]));

        // Assert
        type.Should().Be("BLOB");
    }

    [Fact]
    public void GetDataTypeMapping_WithGuid_ShouldReturnText()
    {
        // Act
        var type = _dialect.GetDataTypeMapping(typeof(Guid));

        // Assert
        type.Should().Be("TEXT");
    }

    [Fact]
    public void GetNextSequenceValueSql_ShouldThrowNotSupportedException()
    {
        // Act
        Action act = () => _dialect.GetNextSequenceValueSql("seq_users");

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*does not support sequences*");
    }

    [Fact]
    public void GetCreateTableValuedParameterTypeSql_ShouldThrowNotSupportedException()
    {
        // Act
        Action act = () => _dialect.GetCreateTableValuedParameterTypeSql("UserType", new[] { "Id INT", "Name TEXT" });

        // Assert
        act.Should().Throw<NotSupportedException>()
            .WithMessage("*does not support table-valued parameters*");
    }

    [Fact]
    public void GetFullTextSearchSql_ShouldUseFTS5()
    {
        // Act
        var sql = _dialect.GetFullTextSearchSql("users", new[] { "Username", "Email" });

        // Assert
        sql.Should().ContainEquivalentOf("fts5"); // Case-insensitive check
        sql.Should().Contain("fts_users");
        sql.Should().Contain("\"Username\"");
        sql.Should().Contain("\"Email\"");
    }
}

