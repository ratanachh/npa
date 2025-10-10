using NPA.Core.Providers;

namespace NPA.Providers.Sqlite;

/// <summary>
/// SQLite-specific type converter implementation.
/// SQLite has a dynamic type system with type affinity: INTEGER, REAL, TEXT, BLOB, NULL.
/// </summary>
public class SqliteTypeConverter : ITypeConverter
{
    /// <inheritdoc />
    public string GetDatabaseTypeName(Type clrType, int? length = null, int? precision = null, int? scale = null)
    {
        if (clrType == null)
            throw new ArgumentNullException(nameof(clrType));

        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        // SQLite type affinity mappings
        return actualType switch
        {
            // Integer types - all map to INTEGER
            Type t when t == typeof(byte) => "INTEGER",
            Type t when t == typeof(sbyte) => "INTEGER",
            Type t when t == typeof(short) => "INTEGER",
            Type t when t == typeof(ushort) => "INTEGER",
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(uint) => "INTEGER",
            Type t when t == typeof(long) => "INTEGER",
            Type t when t == typeof(ulong) => "INTEGER",
            Type t when t == typeof(bool) => "INTEGER", // 0 or 1
            
            // Floating point types - map to REAL
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(double) => "REAL",
            Type t when t == typeof(decimal) => "REAL", // Note: SQLite REAL is approximate
            
            // Date/Time types - stored as TEXT in ISO8601 format
            Type t when t == typeof(DateTime) => "TEXT",
            Type t when t == typeof(DateTimeOffset) => "TEXT",
            Type t when t == typeof(TimeSpan) => "TEXT",
            Type t when t == typeof(DateOnly) => "TEXT",
            Type t when t == typeof(TimeOnly) => "TEXT",
            
            // String types
            Type t when t == typeof(string) => "TEXT",
            Type t when t == typeof(char) => "TEXT",
            
            // GUID - stored as TEXT
            Type t when t == typeof(Guid) => "TEXT",
            
            // Binary data
            Type t when t == typeof(byte[]) => "BLOB",
            
            // Default to TEXT
            _ => "TEXT"
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
            // Boolean - SQLite stores as INTEGER (0 or 1)
            Type t when t == typeof(bool) => (bool)value ? 1L : 0L,
            
            // DateTime - store as ISO8601 string
            Type t when t == typeof(DateTime) => ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss.fff"),
            
            // DateTimeOffset - store as ISO8601 string with offset
            Type t when t == typeof(DateTimeOffset) => ((DateTimeOffset)value).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz"),
            
            // TimeSpan - store as ISO8601 duration string
            Type t when t == typeof(TimeSpan) => ((TimeSpan)value).ToString("c"),
            
            // DateOnly - store as ISO8601 date string
            Type t when t == typeof(DateOnly) => ((DateOnly)value).ToString("yyyy-MM-dd"),
            
            // TimeOnly - store as ISO8601 time string
            Type t when t == typeof(TimeOnly) => ((TimeOnly)value).ToString("HH:mm:ss.fff"),
            
            // Guid - store as string
            Type t when t == typeof(Guid) => ((Guid)value).ToString(),
            
            // Decimal - SQLite uses REAL (approximate)
            Type t when t == typeof(decimal) => (double)(decimal)value,
            
            // All other types - pass through
            _ => value
        };
    }

    /// <inheritdoc />
    public object? ConvertFromDatabase(object? value, Type targetType)
    {
        if (value == null || value is DBNull)
            return null;

        if (targetType == null)
            throw new ArgumentNullException(nameof(targetType));

        // Handle nullable types
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // If types match, return as-is
        if (value.GetType() == actualType)
            return value;

        return actualType switch
        {
            // Boolean - SQLite stores as INTEGER
            Type t when t == typeof(bool) => Convert.ToInt64(value) != 0,
            
            // DateTime - parse from ISO8601 string or integer (Unix timestamp)
            Type t when t == typeof(DateTime) => value switch
            {
                string str => DateTime.Parse(str),
                long unixTime => DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime,
                _ => Convert.ToDateTime(value)
            },
            
            // DateTimeOffset - parse from ISO8601 string with offset
            Type t when t == typeof(DateTimeOffset) => value switch
            {
                string str => DateTimeOffset.Parse(str),
                long unixTime => DateTimeOffset.FromUnixTimeSeconds(unixTime),
                _ => new DateTimeOffset(Convert.ToDateTime(value))
            },
            
            // TimeSpan - parse from string
            Type t when t == typeof(TimeSpan) => value switch
            {
                string str => TimeSpan.Parse(str),
                _ => TimeSpan.FromSeconds(Convert.ToDouble(value))
            },
            
            // DateOnly - parse from string
            Type t when t == typeof(DateOnly) => value switch
            {
                string str => DateOnly.Parse(str),
                _ => DateOnly.FromDateTime(Convert.ToDateTime(value))
            },
            
            // TimeOnly - parse from string
            Type t when t == typeof(TimeOnly) => value switch
            {
                string str => TimeOnly.Parse(str),
                _ => TimeOnly.FromDateTime(Convert.ToDateTime(value))
            },
            
            // Guid - parse from string
            Type t when t == typeof(Guid) => value switch
            {
                string str => Guid.Parse(str),
                byte[] bytes => new Guid(bytes),
                _ => Guid.Parse(value.ToString()!)
            },
            
            // Decimal - SQLite uses REAL
            Type t when t == typeof(decimal) => Convert.ToDecimal(value),
            
            // Use standard type conversion for all other types
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

        // SQLite boolean handling - null vs 0 vs 1
        if (type == typeof(bool) || type == typeof(bool?))
            return true;

        // DateTime handling - need to preserve format
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return true;

        return false;
    }
}


