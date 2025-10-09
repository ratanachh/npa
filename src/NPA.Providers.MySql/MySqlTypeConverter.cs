using NPA.Core.Providers;

namespace NPA.Providers.MySql;

/// <summary>
/// MySQL/MariaDB-specific type converter implementation.
/// </summary>
public class MySqlTypeConverter : ITypeConverter
{
    /// <inheritdoc />
    public string GetDatabaseTypeName(Type clrType, int? length = null, int? precision = null, int? scale = null)
    {
        if (clrType == null)
            throw new ArgumentNullException(nameof(clrType));

        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        return actualType switch
        {
            // Integer types
            Type t when t == typeof(byte) => "TINYINT UNSIGNED",
            Type t when t == typeof(sbyte) => "TINYINT",
            Type t when t == typeof(short) => "SMALLINT",
            Type t when t == typeof(ushort) => "SMALLINT UNSIGNED",
            Type t when t == typeof(int) => "INT",
            Type t when t == typeof(uint) => "INT UNSIGNED",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(ulong) => "BIGINT UNSIGNED",
            
            // Floating point types
            Type t when t == typeof(float) => "FLOAT",
            Type t when t == typeof(double) => "DOUBLE",
            Type t when t == typeof(decimal) => precision.HasValue && scale.HasValue 
                ? $"DECIMAL({precision},{scale})" 
                : precision.HasValue 
                    ? $"DECIMAL({precision},0)" 
                    : "DECIMAL(18,2)",
            
            // Boolean - MySQL uses TINYINT(1)
            Type t when t == typeof(bool) => "TINYINT(1)",
            
            // Date/Time types
            Type t when t == typeof(DateTime) => "DATETIME",
            Type t when t == typeof(DateTimeOffset) => "DATETIME", // Store as UTC, lose offset
            Type t when t == typeof(TimeSpan) => "TIME",
            Type t when t == typeof(DateOnly) => "DATE",
            Type t when t == typeof(TimeOnly) => "TIME",
            
            // String types
            Type t when t == typeof(string) => length.HasValue 
                ? length.Value == -1 
                    ? "TEXT" 
                    : length.Value > 65535
                        ? "MEDIUMTEXT"
                        : length.Value > 255
                            ? "TEXT"
                            : $"VARCHAR({length})"
                : "VARCHAR(255)",
            Type t when t == typeof(char) => "CHAR(1)",
            
            // GUID - MySQL stores as CHAR(36) or BINARY(16)
            Type t when t == typeof(Guid) => "CHAR(36)",
            
            // Binary data
            Type t when t == typeof(byte[]) => length.HasValue 
                ? length.Value == -1 
                    ? "LONGBLOB" 
                    : length.Value > 65535
                        ? "MEDIUMBLOB"
                        : $"VARBINARY({length})"
                : "BLOB",
            
            // MySQL specific types
            Type t when t.Name == "MySqlGeometry" => "GEOMETRY",
            Type t when t.Name == "MySqlPoint" => "POINT",
            Type t when t.Name == "MySqlLineString" => "LINESTRING",
            Type t when t.Name == "MySqlPolygon" => "POLYGON",
            
            // Default
            _ => "VARCHAR(255)"
        };
    }

    /// <inheritdoc />
    public object? ConvertToDatabase(object? value, Type targetType)
    {
        if (value == null)
            return DBNull.Value;

        if (targetType == null)
            throw new ArgumentNullException(nameof(targetType));

        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return actualType switch
        {
            // Boolean - MySQL stores as TINYINT(1)
            Type t when t == typeof(bool) => (bool)value ? (byte)1 : (byte)0,
            
            // DateTime - ensure proper format
            Type t when t == typeof(DateTime) => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss"),
            
            // DateTimeOffset - convert to UTC DateTime
            Type t when t == typeof(DateTimeOffset) => ((DateTimeOffset)value).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
            
            // TimeSpan - format as TIME
            Type t when t == typeof(TimeSpan) => ((TimeSpan)value).ToString(@"hh\:mm\:ss"),
            
            // DateOnly - format as DATE
            Type t when t == typeof(DateOnly) => ((DateOnly)value).ToString("yyyy-MM-dd"),
            
            // TimeOnly - format as TIME
            Type t when t == typeof(TimeOnly) => ((TimeOnly)value).ToString("HH:mm:ss"),
            
            // Guid - format as string
            Type t when t == typeof(Guid) => ((Guid)value).ToString(),
            
            // All other types pass through
            _ => value
        };
    }

    /// <inheritdoc />
    public object? ConvertFromDatabase(object? value, Type targetType)
    {
        if (value == null || value is DBNull)
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        if (targetType == null)
            throw new ArgumentNullException(nameof(targetType));

        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return actualType switch
        {
            // Boolean - MySQL returns TINYINT(1)
            Type t when t == typeof(bool) => Convert.ToBoolean(value),
            
            // Guid - convert from string
            Type t when t == typeof(Guid) => value is string str ? Guid.Parse(str) : value,
            
            // DateTimeOffset - convert from DateTime (assume UTC)
            Type t when t == typeof(DateTimeOffset) => value is DateTime dt 
                ? new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc))
                : value,
            
            // All other types pass through or use Convert
            _ => Convert.ChangeType(value, actualType)
        };
    }

    /// <inheritdoc />
    public bool SupportsType(Type type)
    {
        if (type == null)
            return false;

        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType.IsPrimitive
            || actualType == typeof(string)
            || actualType == typeof(decimal)
            || actualType == typeof(DateTime)
            || actualType == typeof(DateTimeOffset)
            || actualType == typeof(TimeSpan)
            || actualType == typeof(DateOnly)
            || actualType == typeof(TimeOnly)
            || actualType == typeof(Guid)
            || actualType == typeof(byte[]);
    }

    /// <inheritdoc />
    public object? GetDefaultValue(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        if (!type.IsValueType)
            return null;

        return Activator.CreateInstance(type);
    }

    /// <inheritdoc />
    public bool RequiresSpecialNullHandling(object? value, Type type)
    {
        if (value == null)
            return true;

        // MySQL boolean handling - null vs 0 vs 1
        if (type == typeof(bool) || type == typeof(bool?))
            return true;

        return false;
    }
}

