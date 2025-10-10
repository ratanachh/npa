using FluentAssertions;
using NPA.Providers.PostgreSql;
using Xunit;

namespace NPA.Providers.PostgreSql.Tests;

/// <summary>
/// Tests for PostgreSQL type converter implementation.
/// </summary>
public class PostgreSqlTypeConverterTests
{
    private readonly PostgreSqlTypeConverter _converter;

    public PostgreSqlTypeConverterTests()
    {
        _converter = new PostgreSqlTypeConverter();
    }

    [Theory]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(string), true)]
    [InlineData(typeof(bool), true)]
    [InlineData(typeof(DateTime), true)]
    [InlineData(typeof(Guid), true)]
    [InlineData(typeof(decimal), true)]
    public void SupportsType_WithCommonTypes_ShouldReturnTrue(Type type, bool expected)
    {
        // Act
        var result = _converter.SupportsType(type);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void SupportsType_WithNullType_ShouldReturnFalse()
    {
        // Act
        var result = _converter.SupportsType(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ConvertToDatabase_WithNull_ShouldReturnDBNull()
    {
        // Act
        var result = _converter.ConvertToDatabase(null, typeof(string));

        // Assert
        result.Should().Be(DBNull.Value);
    }

    [Fact]
    public void ConvertToDatabase_WithString_ShouldReturnSameValue()
    {
        // Arrange
        var value = "test string";

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(string));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertToDatabase_WithInt_ShouldReturnSameValue()
    {
        // Arrange
        var value = 42;

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(int));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void ConvertToDatabase_WithDateTime_ShouldSpecifyUtcKind()
    {
        // Arrange
        var value = new DateTime(2025, 10, 10, 12, 0, 0, DateTimeKind.Unspecified);

        // Act
        var result = _converter.ConvertToDatabase(value, typeof(DateTime));

        // Assert
        result.Should().BeOfType<DateTime>();
        var dt = (DateTime)result!;
        dt.Kind.Should().Be(DateTimeKind.Utc);
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
    public void ConvertFromDatabase_WithDBNull_ShouldReturnNull()
    {
        // Act
        var result = _converter.ConvertFromDatabase(DBNull.Value, typeof(string));

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData("t", true)]
    [InlineData("true", true)]
    [InlineData("1", true)]
    [InlineData("f", false)]
    [InlineData("false", false)]
    [InlineData("0", false)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void ConvertFromDatabase_WithBooleanValues_ShouldConvertCorrectly(object value, bool expected)
    {
        // Act
        var result = _converter.ConvertFromDatabase(value, typeof(bool));

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertFromDatabase_WithGuidString_ShouldConvertToGuid()
    {
        // Arrange
        var guidString = "550e8400-e29b-41d4-a716-446655440000";

        // Act
        var result = _converter.ConvertFromDatabase(guidString, typeof(Guid));

        // Assert
        result.Should().BeOfType<Guid>();
        result.Should().Be(Guid.Parse(guidString));
    }

    [Fact]
    public void ConvertFromDatabase_WithGuidBytes_ShouldConvertToGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var bytes = guid.ToByteArray();

        // Act
        var result = _converter.ConvertFromDatabase(bytes, typeof(Guid));

        // Assert
        result.Should().BeOfType<Guid>();
        result.Should().Be(guid);
    }

    [Fact]
    public void ConvertFromDatabase_WithDateTimeString_ShouldConvertToDateTime()
    {
        // Arrange
        var dateString = "2025-10-10 12:00:00";

        // Act
        var result = _converter.ConvertFromDatabase(dateString, typeof(DateTime));

        // Assert
        result.Should().BeOfType<DateTime>();
        var dt = (DateTime)result!;
        dt.Year.Should().Be(2025);
        dt.Month.Should().Be(10);
        dt.Day.Should().Be(10);
    }

    [Theory]
    [InlineData(typeof(int), "INTEGER")]
    [InlineData(typeof(long), "BIGINT")]
    [InlineData(typeof(short), "SMALLINT")]
    [InlineData(typeof(byte), "SMALLINT")]
    [InlineData(typeof(bool), "BOOLEAN")]
    [InlineData(typeof(string), "TEXT")]
    [InlineData(typeof(DateTime), "TIMESTAMP")]
    [InlineData(typeof(DateTimeOffset), "TIMESTAMP WITH TIME ZONE")]
    [InlineData(typeof(Guid), "UUID")]
    [InlineData(typeof(decimal), "NUMERIC")]
    [InlineData(typeof(double), "DOUBLE PRECISION")]
    [InlineData(typeof(float), "REAL")]
    public void GetDatabaseTypeName_WithBasicTypes_ShouldReturnCorrectType(Type type, string expectedType)
    {
        // Act
        var result = _converter.GetDatabaseTypeName(type);

        // Assert
        result.Should().Be(expectedType);
    }

    [Fact]
    public void GetDatabaseTypeName_WithStringAndLength_ShouldReturnVarchar()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(string), length: 100);

        // Assert
        result.Should().Be("VARCHAR(100)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithDecimalAndPrecisionScale_ShouldReturnNumeric()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(decimal), precision: 18, scale: 2);

        // Assert
        result.Should().Be("NUMERIC(18,2)");
    }

    [Fact]
    public void GetDatabaseTypeName_WithNullableInt_ShouldReturnInteger()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(int?));

        // Assert
        result.Should().Be("INTEGER");
    }

    [Fact]
    public void GetDatabaseTypeName_WithArray_ShouldReturnArrayType()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(int[]));

        // Assert
        result.Should().Be("INTEGER[]");
    }

    [Fact]
    public void GetDatabaseTypeName_WithNullType_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _converter.GetDatabaseTypeName(null!));
    }

    [Fact]
    public void GetDefaultValue_WithValueType_ShouldReturnDefaultInstance()
    {
        // Act
        var result = _converter.GetDefaultValue(typeof(int));

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void GetDefaultValue_WithReferenceType_ShouldReturnNull()
    {
        // Act
        var result = _converter.GetDefaultValue(typeof(string));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDefaultValue_WithNullType_ShouldThrowException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _converter.GetDefaultValue(null!));
    }

    [Theory]
    [InlineData(null, typeof(string), true)]
    [InlineData("2000-01-01", typeof(DateTime), false)] // DateTime.MinValue would be true
    public void RequiresSpecialNullHandling_ShouldDetectSpecialCases(object? value, Type type, bool expected)
    {
        // Act
        var result = _converter.RequiresSpecialNullHandling(value, type);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void RequiresSpecialNullHandling_WithDateTimeMinValue_ShouldReturnTrue()
    {
        // Act
        var result = _converter.RequiresSpecialNullHandling(DateTime.MinValue, typeof(DateTime));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresSpecialNullHandling_WithGuidEmpty_ShouldReturnTrue()
    {
        // Act
        var result = _converter.RequiresSpecialNullHandling(Guid.Empty, typeof(Guid));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsType_WithArrayType_ShouldReturnTrue()
    {
        // Act
        var result = _converter.SupportsType(typeof(int[]));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void SupportsType_WithEnumType_ShouldReturnTrue()
    {
        // Act
        var result = _converter.SupportsType(typeof(TestEnum));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetDatabaseTypeName_WithEnum_ShouldReturnInteger()
    {
        // Act
        var result = _converter.GetDatabaseTypeName(typeof(TestEnum));

        // Assert
        result.Should().Be("INTEGER");
    }

    [Fact]
    public void ConvertFromDatabase_WithIntegerToInt_ShouldConvert()
    {
        // Arrange
        var value = 42;

        // Act
        var result = _converter.ConvertFromDatabase(value, typeof(int));

        // Assert
        result.Should().Be(42);
    }

    [Fact]
    public void ConvertFromDatabase_WithLongToLong_ShouldConvert()
    {
        // Arrange
        long value = 123456789L;

        // Act
        var result = _converter.ConvertFromDatabase(value, typeof(long));

        // Assert
        result.Should().Be(123456789L);
    }

    private enum TestEnum
    {
        Value1,
        Value2,
        Value3
    }
}

