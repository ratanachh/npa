using FluentAssertions;
using NPA.Providers.SqlServer;
using Xunit;

namespace NPA.Providers.SqlServer.Tests;

/// <summary>
/// Tests for SQL Server type converter implementation.
/// </summary>
public class SqlServerTypeConverterTests
{
    private readonly SqlServerTypeConverter _converter;

    public SqlServerTypeConverterTests()
    {
        _converter = new SqlServerTypeConverter();
    }

    [Theory]
    [InlineData(typeof(int), "INT")]
    [InlineData(typeof(long), "BIGINT")]
    [InlineData(typeof(short), "SMALLINT")]
    [InlineData(typeof(byte), "TINYINT")]
    [InlineData(typeof(bool), "BIT")]
    [InlineData(typeof(DateTime), "DATETIME2")]
    [InlineData(typeof(decimal), "DECIMAL(18,2)")]
    [InlineData(typeof(float), "REAL")]
    [InlineData(typeof(double), "FLOAT")]
    [InlineData(typeof(Guid), "UNIQUEIDENTIFIER")]
    public void GetDatabaseTypeName_WithBasicTypes_ShouldReturnCorrectSqlType(Type clrType, string expected)
    {
        // Act
        var result = _converter.GetDatabaseTypeName(clrType);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetDatabaseTypeName_WithString_ShouldReturnNVarChar()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(string));

        // Assert
        result.Should().Be("NVARCHAR(255)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithStringAndLength_ShouldReturnNVarCharWithLength()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(string), length: 50);

        // Assert
        result.Should().Be("NVARCHAR(50)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithDecimalAndPrecisionScale_ShouldReturnDecimalWithPrecisionScale()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(decimal), precision: 10, scale: 4);

        // Assert
        result.Should().Be("DECIMAL(10,4)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithNullableInt_ShouldReturnInt()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(int?));

        // Assert
        result.Should().Be("INT");
    }

    [Fact]
    public void GetDatabaseTypeName_WithNullableBool_ShouldReturnBit()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(bool?));

        // Assert
        result.Should().Be("BIT");
    }

    [Fact]
    public void GetDatabaseTypeName_WithByteArray_ShouldReturnVarBinary()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(byte[]));

        // Assert
        result.Should().Be("VARBINARY(MAX)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithUnsupportedType_ShouldReturnNVarCharMax()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(object));

        // Assert
        result.Should().Be("NVARCHAR(MAX)");
    }

    [Fact]
    public void ConvertToDatabase_WithInt_ShouldReturnSameValue()
    {
        // Arrange
        int value = 42;

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(int));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertToDatabase_WithString_ShouldReturnSameValue()
    {
        // Arrange
        string value = "test";

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(string));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertToDatabase_WithNull_ShouldReturnDbNull()
    {
        // Act
        var result = _converter.ConvertToDatabase(null, typeof(string));

        // Assert
        result.Should().Be(DBNull.Value);
    }

    [Fact]
    public void ConvertToDatabase_WithDateTime_ShouldReturnDateTime()
    {
        // Arrange
        var value = new DateTime(2024, 1, 1, 12, 0, 0);

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(DateTime));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertToDatabase_WithBoolTrue_ShouldReturnTrue()
    {
        // Act
        var result = _converter.ConvertToDatabase(true, typeof(bool));

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertToDatabase_WithBoolFalse_ShouldReturnFalse()
    {
        // Act
        var result = _converter.ConvertToDatabase(false, typeof(bool));

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertToDatabase_WithGuid_ShouldReturnSameGuid()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(Guid));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertToDatabase_WithDecimal_ShouldReturnSameValue()
    {
        // Arrange
        decimal value = 123.45m;

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(decimal));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertToDatabase_WithByteArray_ShouldReturnSameArray()
    {
        // Arrange
        byte[] value = { 1, 2, 3, 4, 5 };

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(byte[]));

        // Assert
        result.Should().BeEquivalentTo(value);
    }
}

