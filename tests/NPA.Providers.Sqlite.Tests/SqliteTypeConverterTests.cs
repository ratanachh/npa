using FluentAssertions;
using NPA.Providers.Sqlite;
using Xunit;

namespace NPA.Providers.Sqlite.Tests;

/// <summary>
/// Tests for SQLite type converter.
/// </summary>
public class SqliteTypeConverterTests
{
    private readonly SqliteTypeConverter _typeConverter;

    public SqliteTypeConverterTests()
    {
        _typeConverter = new SqliteTypeConverter();
    }

    [Theory]
    [InlineData(typeof(int), "INTEGER")]
    [InlineData(typeof(long), "INTEGER")]
    [InlineData(typeof(short), "INTEGER")]
    [InlineData(typeof(byte), "INTEGER")]
    [InlineData(typeof(bool), "INTEGER")]
    public void GetDatabaseTypeName_WithIntegerTypes_ShouldReturnInteger(Type clrType, string expected)
    {
        // Act
        var typeName = _typeConverter.GetDatabaseTypeName(clrType);

        // Assert
        typeName.Should().Be(expected);
    }

    [Theory]
    [InlineData(typeof(float), "REAL")]
    [InlineData(typeof(double), "REAL")]
    [InlineData(typeof(decimal), "REAL")]
    public void GetDatabaseTypeName_WithFloatingTypes_ShouldReturnReal(Type clrType, string expected)
    {
        // Act
        var typeName = _typeConverter.GetDatabaseTypeName(clrType);

        // Assert
        typeName.Should().Be(expected);
    }

    [Theory]
    [InlineData(typeof(string), "TEXT")]
    [InlineData(typeof(DateTime), "TEXT")]
    [InlineData(typeof(Guid), "TEXT")]
    public void GetDatabaseTypeName_WithTextTypes_ShouldReturnText(Type clrType, string expected)
    {
        // Act
        var typeName = _typeConverter.GetDatabaseTypeName(clrType);

        // Assert
        typeName.Should().Be(expected);
    }

    [Fact]
    public void GetDatabaseTypeName_WithByteArray_ShouldReturnBlob()
    {
        // Act
        var typeName = _typeConverter.GetDatabaseTypeName(typeof(byte[]));

        // Assert
        typeName.Should().Be("BLOB");
    }

    [Fact]
    public void ConvertToDatabase_WithTrue_ShouldReturn1()
    {
        // Act
        var converted = _typeConverter.ConvertToDatabase(true, typeof(bool));

        // Assert
        converted.Should().Be(1L);
    }

    [Fact]
    public void ConvertToDatabase_WithFalse_ShouldReturn0()
    {
        // Act
        var converted = _typeConverter.ConvertToDatabase(false, typeof(bool));

        // Assert
        converted.Should().Be(0L);
    }

    [Fact]
    public void ConvertToDatabase_WithDateTime_ShouldReturnISO8601String()
    {
        // Arrange
        var dateTime = new DateTime(2024, 10, 10, 14, 30, 0);

        // Act
        var converted = _typeConverter.ConvertToDatabase(dateTime, typeof(DateTime));

        // Assert
        converted.Should().BeOfType<string>();
        converted.Should().Be("2024-10-10 14:30:00.000");
    }

    [Fact]
    public void ConvertToDatabase_WithGuid_ShouldReturnString()
    {
        // Arrange
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var converted = _typeConverter.ConvertToDatabase(guid, typeof(Guid));

        // Assert
        converted.Should().BeOfType<string>();
        converted.Should().Be("12345678-1234-1234-1234-123456789012");
    }

    [Fact]
    public void ConvertToDatabase_WithNull_ShouldReturnDBNull()
    {
        // Act
        var converted = _typeConverter.ConvertToDatabase(null, typeof(string));

        // Assert
        converted.Should().Be(DBNull.Value);
    }

    [Fact]
    public void ConvertFromDatabase_WithInteger1_ShouldReturnTrue()
    {
        // Act
        var converted = _typeConverter.ConvertFromDatabase(1L, typeof(bool));

        // Assert
        converted.Should().Be(true);
    }

    [Fact]
    public void ConvertFromDatabase_WithInteger0_ShouldReturnFalse()
    {
        // Act
        var converted = _typeConverter.ConvertFromDatabase(0L, typeof(bool));

        // Assert
        converted.Should().Be(false);
    }

    [Fact]
    public void ConvertFromDatabase_WithISO8601String_ShouldReturnDateTime()
    {
        // Arrange
        var dateString = "2024-10-10 14:30:00.000";

        // Act
        var converted = _typeConverter.ConvertFromDatabase(dateString, typeof(DateTime));

        // Assert
        converted.Should().BeOfType<DateTime>();
        var dateTime = (DateTime)converted!;
        dateTime.Year.Should().Be(2024);
        dateTime.Month.Should().Be(10);
        dateTime.Day.Should().Be(10);
    }

    [Fact]
    public void ConvertFromDatabase_WithGuidString_ShouldReturnGuid()
    {
        // Arrange
        var guidString = "12345678-1234-1234-1234-123456789012";

        // Act
        var converted = _typeConverter.ConvertFromDatabase(guidString, typeof(Guid));

        // Assert
        converted.Should().BeOfType<Guid>();
        converted.Should().Be(Guid.Parse(guidString));
    }

    [Fact]
    public void ConvertFromDatabase_WithNull_ShouldReturnNull()
    {
        // Act
        var converted = _typeConverter.ConvertFromDatabase(null, typeof(string));

        // Assert
        converted.Should().BeNull();
    }

    [Fact]
    public void ConvertFromDatabase_WithDBNull_ShouldReturnNull()
    {
        // Act
        var converted = _typeConverter.ConvertFromDatabase(DBNull.Value, typeof(string));

        // Assert
        converted.Should().BeNull();
    }

    [Fact]
    public void SupportsType_WithSupportedType_ShouldReturnTrue()
    {
        // Assert
        _typeConverter.SupportsType(typeof(int)).Should().BeTrue();
        _typeConverter.SupportsType(typeof(string)).Should().BeTrue();
        _typeConverter.SupportsType(typeof(DateTime)).Should().BeTrue();
        _typeConverter.SupportsType(typeof(bool)).Should().BeTrue();
        _typeConverter.SupportsType(typeof(Guid)).Should().BeTrue();
        _typeConverter.SupportsType(typeof(byte[])).Should().BeTrue();
    }

    [Fact]
    public void GetDefaultValue_WithValueType_ShouldReturnDefault()
    {
        // Act
        var defaultInt = _typeConverter.GetDefaultValue(typeof(int));
        var defaultBool = _typeConverter.GetDefaultValue(typeof(bool));

        // Assert
        defaultInt.Should().Be(0);
        defaultBool.Should().Be(false);
    }

    [Fact]
    public void GetDefaultValue_WithReferenceType_ShouldReturnNull()
    {
        // Act
        var defaultString = _typeConverter.GetDefaultValue(typeof(string));

        // Assert
        defaultString.Should().BeNull();
    }

    [Fact]
    public void RequiresSpecialNullHandling_WithNull_ShouldReturnTrue()
    {
        // Act
        var result = _typeConverter.RequiresSpecialNullHandling(null, typeof(string));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresSpecialNullHandling_WithBoolean_ShouldReturnTrue()
    {
        // Act
        var result = _typeConverter.RequiresSpecialNullHandling(true, typeof(bool));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void RequiresSpecialNullHandling_WithDateTime_ShouldReturnTrue()
    {
        // Act
        var result = _typeConverter.RequiresSpecialNullHandling(DateTime.Now, typeof(DateTime));

        // Assert
        result.Should().BeTrue();
    }
}

