using FluentAssertions;
using NPA.Providers.MySql;
using Xunit;

namespace NPA.Providers.MySql.Tests;

/// <summary>
/// Tests for MySQL type converter implementation.
/// </summary>
public class MySqlTypeConverterTests
{
    private readonly MySqlTypeConverter _converter;

    public MySqlTypeConverterTests()
    {
        _converter = new MySqlTypeConverter();
    }

    [Theory]
    [InlineData(typeof(int), "INT")]
    [InlineData(typeof(long), "BIGINT")]
    [InlineData(typeof(short), "SMALLINT")]
    [InlineData(typeof(byte), "TINYINT UNSIGNED")]
    [InlineData(typeof(sbyte), "TINYINT")]
    [InlineData(typeof(bool), "TINYINT(1)")]
    [InlineData(typeof(DateTime), "DATETIME")]
    [InlineData(typeof(decimal), "DECIMAL(18,2)")]
    [InlineData(typeof(float), "FLOAT")]
    [InlineData(typeof(double), "DOUBLE")]
    [InlineData(typeof(Guid), "CHAR(36)")]
    public void GetDatabaseTypeName_WithBasicTypes_ShouldReturnCorrectMySqlType(Type clrType, string expected)
    {
        // Act
        var result = _converter.GetDatabaseTypeName(clrType);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void GetDatabaseTypeName_WithString_ShouldReturnVARCHAR()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(string));

        // Assert
        result.Should().Be("VARCHAR(255)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithStringAndLength_ShouldReturnVARCHARWithLength()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(string), length: 50);

        // Assert
        result.Should().Be("VARCHAR(50)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithLongString_ShouldReturnTEXT()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(string), length: -1);

        // Assert
        result.Should().Be("TEXT");
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
    public void GetDatabaseTypeName_WithNullableBool_ShouldReturnTinyInt()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(bool?));

        // Assert
        result.Should().Be("TINYINT(1)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithByteArray_ShouldReturnBLOB()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(byte[]));

        // Assert
        result.Should().Be("BLOB");
    }

    [Fact]
    public void GetDatabaseTypeName_WithLargeByteArray_ShouldReturnLONGBLOB()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(byte[]), length: -1);

        // Assert
        result.Should().Be("LONGBLOB");
    }

    [Fact]
    public void GetDatabaseTypeName_WithUnsupportedType_ShouldReturnVARCHAR()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(object));

        // Assert
        result.Should().Be("VARCHAR(255)");
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
    public void ConvertToDatabase_WithBoolTrue_ShouldReturnOne()
    {
        // Act
        var result = _converter.ConvertToDatabase(true, typeof(bool));

        // Assert
        result.Should().Be((byte)1);
    }

    [Fact]
    public void ConvertToDatabase_WithBoolFalse_ShouldReturnZero()
    {
        // Act
        var result = _converter.ConvertToDatabase(false, typeof(bool));

        // Assert
        result.Should().Be((byte)0);
    }

    [Fact]
    public void ConvertToDatabase_WithGuid_ShouldReturnString()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(Guid));

        // Assert
        result.Should().BeOfType<string>();
        result.Should().Be(value.ToString());
    }

    [Fact]
    public void ConvertToDatabase_WithDateTime_ShouldReturnFormattedString()
    {
        // Arrange
        var value = new DateTime(2024, 1, 1, 12, 30, 45);

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(DateTime));

        // Assert
        result.Should().Be("2024-01-01 12:30:45");
    }

    [Fact]
    public void ConvertToDatabase_WithDateOnly_ShouldReturnFormattedString()
    {
        // Arrange
        var value = new DateOnly(2024, 1, 1);

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(DateOnly));

        // Assert
        result.Should().Be("2024-01-01");
    }

    [Fact]
    public void ConvertToDatabase_WithTimeOnly_ShouldReturnFormattedString()
    {
        // Arrange
        var value = new TimeOnly(12, 30, 45);

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(TimeOnly));

        // Assert
        result.Should().Be("12:30:45");
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

    [Fact]
    public void ConvertFromDatabase_WithNull_ShouldReturnNull()
    {
        // Act
        var result = _converter.ConvertFromDatabase(null, typeof(string));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertFromDatabase_WithDbNull_ShouldReturnNull()
    {
        // Act
        var result = _converter.ConvertFromDatabase(DBNull.Value, typeof(string));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ConvertFromDatabase_WithBoolOne_ShouldReturnTrue()
    {
        // Act
        var result = _converter.ConvertFromDatabase((byte)1, typeof(bool));

        // Assert
        result.Should().Be(true);
    }

    [Fact]
    public void ConvertFromDatabase_WithBoolZero_ShouldReturnFalse()
    {
        // Act
        var result = _converter.ConvertFromDatabase((byte)0, typeof(bool));

        // Assert
        result.Should().Be(false);
    }

    [Fact]
    public void ConvertFromDatabase_WithGuidString_ShouldReturnGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var result = _converter.ConvertFromDatabase(guidString, typeof(Guid));

        // Assert
        result.Should().Be(guid);
    }

    [Fact]
    public void GetDataTypeMapping_WithUnsignedTypes_ShouldReturnUNSIGNED()
    {
        // Act & Assert
        _converter.GetDatabaseTypeName(typeof(uint)).Should().Be("INT UNSIGNED");
        _converter.GetDatabaseTypeName(typeof(ulong)).Should().Be("BIGINT UNSIGNED");
        _converter.GetDatabaseTypeName(typeof(ushort)).Should().Be("SMALLINT UNSIGNED");
    }
}

