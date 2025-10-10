using System.Text.Json;
using NPA.Core.Providers;
using NpgsqlTypes;

namespace NPA.Providers.PostgreSql;

/// <summary>
/// PostgreSQL-specific type converter implementation.
/// </summary>
public class PostgreSqlTypeConverter : ITypeConverter
{
    private static readonly Dictionary<Type, string> TypeMappings = new()
    {
        // Integer types
        { typeof(byte), "SMALLINT" },
        { typeof(byte?), "SMALLINT" },
        { typeof(short), "SMALLINT" },
        { typeof(short?), "SMALLINT" },
        { typeof(int), "INTEGER" },
        { typeof(int?), "INTEGER" },
        { typeof(long), "BIGINT" },
        { typeof(long?), "BIGINT" },
        
        // Floating point types
        { typeof(float), "REAL" },
        { typeof(float?), "REAL" },
        { typeof(double), "DOUBLE PRECISION" },
        { typeof(double?), "DOUBLE PRECISION" },
        { typeof(decimal), "NUMERIC" },
        { typeof(decimal?), "NUMERIC" },
        
        // Boolean
        { typeof(bool), "BOOLEAN" },
        { typeof(bool?), "BOOLEAN" },
        
        // Date/Time types
        { typeof(DateTime), "TIMESTAMP" },
        { typeof(DateTime?), "TIMESTAMP" },
        { typeof(DateTimeOffset), "TIMESTAMP WITH TIME ZONE" },
        { typeof(DateTimeOffset?), "TIMESTAMP WITH TIME ZONE" },
        { typeof(TimeSpan), "INTERVAL" },
        { typeof(TimeSpan?), "INTERVAL" },
        
        // String types
        { typeof(string), "TEXT" },
        { typeof(char), "CHAR(1)" },
        { typeof(char?), "CHAR(1)" },
        
        // GUID
        { typeof(Guid), "UUID" },
        { typeof(Guid?), "UUID" },
        
        // Binary data
        { typeof(byte[]), "BYTEA" }
    };

    /// <inheritdoc />
    public object? ConvertToDatabase(object? value, Type targetType)
    {
        if (value == null)
            return DBNull.Value;

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle special PostgreSQL types
        return actualType switch
        {
            // Convert .NET DateTime to PostgreSQL timestamp
            Type t when t == typeof(DateTime) && value is DateTime dateTime => 
                dateTime.Kind == DateTimeKind.Unspecified 
                    ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
                    : dateTime,
            
            // Convert .NET DateTimeOffset to PostgreSQL timestamptz
            Type t when t == typeof(DateTimeOffset) && value is DateTimeOffset dto =>
                dto,
            
            // Handle JSON serialization for complex objects
            Type t when !IsSimpleType(t) && !(value is byte[]) && !(value is Array) => 
                JsonSerializer.Serialize(value),
            
            // Handle arrays
            Type t when t.IsArray && value is Array array =>
                array,
            
            // Handle PostgreSQL-specific types
            Type t when t == typeof(NpgsqlPoint) || 
                        t == typeof(NpgsqlLine) || 
                        t == typeof(NpgsqlPolygon) ||
                        t == typeof(NpgsqlCircle) ||
                        t == typeof(NpgsqlBox) ||
                        t == typeof(NpgsqlPath) =>
                value,
            
            // Handle network address types
            Type t when t.Name == "NpgsqlInet" || t.Name == "NpgsqlCidr" =>
                value,
            
            // Handle range types
            Type t when t.Name.StartsWith("NpgsqlRange") =>
                value,
            
            // Default
            _ => value
        };
    }

    /// <inheritdoc />
    public object? ConvertFromDatabase(object? value, Type targetType)
    {
        if (value == null || value is DBNull)
            return null;

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle special PostgreSQL conversions
        return actualType switch
        {
            // Handle boolean conversions from various PostgreSQL representations
            Type t when t == typeof(bool) => value switch
            {
                bool b => b,
                string s => s.Equals("t", StringComparison.OrdinalIgnoreCase) || 
                           s.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                           s.Equals("1", StringComparison.OrdinalIgnoreCase),
                int i => i != 0,
                _ => Convert.ToBoolean(value)
            },
            
            // Handle UUID conversions
            Type t when t == typeof(Guid) => value switch
            {
                Guid g => g,
                string s => Guid.Parse(s),
                byte[] bytes => new Guid(bytes),
                _ => Guid.Empty
            },
            
            // Handle timestamp conversions
            Type t when t == typeof(DateTime) => value switch
            {
                DateTime dt => dt,
                DateTimeOffset dto => dto.DateTime,
                string s => DateTime.Parse(s),
                _ => Convert.ToDateTime(value)
            },
            
            // Handle timestamptz conversions
            Type t when t == typeof(DateTimeOffset) => value switch
            {
                DateTimeOffset dto => dto,
                DateTime dt => new DateTimeOffset(dt),
                string s => DateTimeOffset.Parse(s),
                _ => new DateTimeOffset(Convert.ToDateTime(value))
            },
            
            // Handle interval conversions
            Type t when t == typeof(TimeSpan) => value switch
            {
                TimeSpan ts => ts,
                NpgsqlInterval interval => TimeSpan.FromTicks(interval.Time * 10),
                string s => TimeSpan.Parse(s),
                _ => TimeSpan.Zero
            },
            
            // Handle JSON deserialization
            Type t when !IsSimpleType(t) && value is string json =>
                JsonSerializer.Deserialize(json, actualType),
            
            // Handle array types
            Type t when t.IsArray && value is Array =>
                value,
            
            // Handle numeric types
            Type t when t == typeof(int) => Convert.ToInt32(value),
            Type t when t == typeof(long) => Convert.ToInt64(value),
            Type t when t == typeof(short) => Convert.ToInt16(value),
            Type t when t == typeof(byte) => Convert.ToByte(value),
            Type t when t == typeof(decimal) => Convert.ToDecimal(value),
            Type t when t == typeof(double) => Convert.ToDouble(value),
            Type t when t == typeof(float) => Convert.ToSingle(value),
            
            // Handle string
            Type t when t == typeof(string) => value.ToString(),
            
            // Handle binary data
            Type t when t == typeof(byte[]) => value as byte[],
            
            // Default
            _ => value
        };
    }

    /// <inheritdoc />
    public string GetDbType(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (TypeMappings.TryGetValue(actualType, out var dbType))
            return dbType;

        // Handle arrays
        if (actualType.IsArray)
        {
            var elementType = actualType.GetElementType();
            if (elementType != null && TypeMappings.TryGetValue(elementType, out var elementDbType))
                return $"{elementDbType}[]";
        }

        // Handle enums
        if (actualType.IsEnum)
            return "INTEGER";

        // Default to TEXT for complex types (will be JSON serialized)
        return "TEXT";
    }

    /// <inheritdoc />
    public bool SupportsType(Type type)
    {
        if (type == null)
            return false;

        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        // Check if it's in our mappings
        if (TypeMappings.ContainsKey(actualType))
            return true;

        // Support arrays
        if (actualType.IsArray)
            return true;

        // Support enums
        if (actualType.IsEnum)
            return true;

        // Support PostgreSQL-specific types
        if (actualType.Namespace?.StartsWith("NpgsqlTypes") == true)
            return true;

        // Support JSON serializable types
        if (!actualType.IsAbstract && !actualType.IsInterface)
            return true;

        return false;
    }

    /// <summary>
    /// Determines whether the specified type is a simple type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>true if the type is a simple type; otherwise, false.</returns>
    private static bool IsSimpleType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType.IsPrimitive ||
               actualType.IsEnum ||
               actualType == typeof(string) ||
               actualType == typeof(decimal) ||
               actualType == typeof(DateTime) ||
               actualType == typeof(DateTimeOffset) ||
               actualType == typeof(TimeSpan) ||
               actualType == typeof(Guid) ||
               actualType == typeof(byte[]);
    }

    /// <inheritdoc />
    public object? GetDefaultValue(Type type)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        if (actualType.IsValueType)
            return Activator.CreateInstance(actualType);

        return null;
    }

    /// <inheritdoc />
    public string GetDatabaseTypeName(Type dotNetType, int? length = null, int? precision = null, int? scale = null)
    {
        if (dotNetType == null)
            throw new ArgumentNullException(nameof(dotNetType));

        var actualType = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

        return actualType switch
        {
            Type t when t == typeof(byte) => "SMALLINT",
            Type t when t == typeof(short) => "SMALLINT",
            Type t when t == typeof(int) => "INTEGER",
            Type t when t == typeof(long) => "BIGINT",
            Type t when t == typeof(float) => "REAL",
            Type t when t == typeof(double) => "DOUBLE PRECISION",
            Type t when t == typeof(decimal) => precision.HasValue && scale.HasValue 
                ? $"NUMERIC({precision},{scale})" 
                : "NUMERIC",
            Type t when t == typeof(bool) => "BOOLEAN",
            Type t when t == typeof(DateTime) => "TIMESTAMP",
            Type t when t == typeof(DateTimeOffset) => "TIMESTAMP WITH TIME ZONE",
            Type t when t == typeof(TimeSpan) => "INTERVAL",
            Type t when t == typeof(string) => length.HasValue ? $"VARCHAR({length})" : "TEXT",
            Type t when t == typeof(char) => "CHAR(1)",
            Type t when t == typeof(Guid) => "UUID",
            Type t when t == typeof(byte[]) => "BYTEA",
            Type t when t.IsArray => $"{GetDatabaseTypeName(t.GetElementType()!)}[]",
            Type t when t.IsEnum => "INTEGER",
            _ => "TEXT"
        };
    }

    /// <inheritdoc />
    public bool RequiresSpecialNullHandling(object? value, Type type)
    {
        if (value == null)
            return true;

        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        // PostgreSQL has some special null handling for certain types
        if (actualType == typeof(DateTime) && value is DateTime dt)
            return dt == DateTime.MinValue;

        if (actualType == typeof(Guid) && value is Guid guid)
            return guid == Guid.Empty;

        return false;
    }

    /// <summary>
    /// Gets the PostgreSQL-specific NpgsqlDbType for a .NET type.
    /// </summary>
    /// <param name="type">The .NET type.</param>
    /// <returns>The corresponding NpgsqlDbType.</returns>
    public NpgsqlDbType? GetNpgsqlDbType(Type type)
    {
        if (type == null)
            return null;

        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType switch
        {
            Type t when t == typeof(short) => NpgsqlDbType.Smallint,
            Type t when t == typeof(int) => NpgsqlDbType.Integer,
            Type t when t == typeof(long) => NpgsqlDbType.Bigint,
            Type t when t == typeof(float) => NpgsqlDbType.Real,
            Type t when t == typeof(double) => NpgsqlDbType.Double,
            Type t when t == typeof(decimal) => NpgsqlDbType.Numeric,
            Type t when t == typeof(bool) => NpgsqlDbType.Boolean,
            Type t when t == typeof(string) => NpgsqlDbType.Text,
            Type t when t == typeof(char) => NpgsqlDbType.Char,
            Type t when t == typeof(DateTime) => NpgsqlDbType.Timestamp,
            Type t when t == typeof(DateTimeOffset) => NpgsqlDbType.TimestampTz,
            Type t when t == typeof(TimeSpan) => NpgsqlDbType.Interval,
            Type t when t == typeof(Guid) => NpgsqlDbType.Uuid,
            Type t when t == typeof(byte[]) => NpgsqlDbType.Bytea,
            Type t when t.IsArray => NpgsqlDbType.Array,
            _ => null
        };
    }
}

