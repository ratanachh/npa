using System.Data.SqlTypes;
using System.Text.Json;
using NPA.Core.Providers;

namespace NPA.Providers.SqlServer;

/// <summary>
/// SQL Server-specific type converter implementation.
/// </summary>
public class SqlServerTypeConverter : ITypeConverter
{
    private static readonly Dictionary<Type, string> TypeMappings = new()
    {
        // Integer types
        { typeof(byte), "TINYINT" },
        { typeof(byte?), "TINYINT" },
        { typeof(short), "SMALLINT" },
        { typeof(short?), "SMALLINT" },
        { typeof(int), "INT" },
        { typeof(int?), "INT" },
        { typeof(long), "BIGINT" },
        { typeof(long?), "BIGINT" },
        
        // Floating point types
        { typeof(float), "REAL" },
        { typeof(float?), "REAL" },
        { typeof(double), "FLOAT" },
        { typeof(double?), "FLOAT" },
        { typeof(decimal), "DECIMAL(18,2)" },
        { typeof(decimal?), "DECIMAL(18,2)" },
        
        // Boolean
        { typeof(bool), "BIT" },
        { typeof(bool?), "BIT" },
        
        // Date/Time types
        { typeof(DateTime), "DATETIME2" },
        { typeof(DateTime?), "DATETIME2" },
        { typeof(DateTimeOffset), "DATETIMEOFFSET" },
        { typeof(DateTimeOffset?), "DATETIMEOFFSET" },
        { typeof(TimeSpan), "TIME" },
        { typeof(TimeSpan?), "TIME" },
        
        // String types
        { typeof(string), "NVARCHAR(255)" },
        { typeof(char), "NCHAR(1)" },
        { typeof(char?), "NCHAR(1)" },
        
        // GUID
        { typeof(Guid), "UNIQUEIDENTIFIER" },
        { typeof(Guid?), "UNIQUEIDENTIFIER" },
        
        // Binary data
        { typeof(byte[]), "VARBINARY(MAX)" }
    };

    /// <inheritdoc />
    public object? ConvertToDatabase(object? value, Type targetType)
    {
        if (value == null)
            return DBNull.Value;

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle special SQL Server types
        return actualType switch
        {
            // Convert .NET DateTime to SqlDateTime if needed
            Type t when t == typeof(DateTime) && value is DateTime dateTime => 
                dateTime == DateTime.MinValue ? SqlDateTime.MinValue.Value : dateTime,
            
            // Convert .NET strings to SqlString for special handling
            Type t when t == typeof(string) && value is string str => 
                string.IsNullOrEmpty(str) ? (object?)null : str,
            
            // Handle JSON serialization for complex objects
            Type t when !IsSimpleType(t) && !(value is byte[]) => 
                JsonSerializer.Serialize(value),
            
            // Handle spatial types (would need Microsoft.SqlServer.Types)
            Type t when t.Name == "SqlGeography" => value,
            Type t when t.Name == "SqlGeometry" => value,
            Type t when t.Name == "SqlHierarchyId" => value,
            
            // Handle TimeSpan conversion to SQL Server TIME
            Type t when t == typeof(TimeSpan) && value is TimeSpan timeSpan =>
                timeSpan.Ticks < 0 ? TimeSpan.Zero : timeSpan,
            
            // Default case - return as is
            _ => value
        };
    }

    /// <inheritdoc />
    public object? ConvertFromDatabase(object? value, Type targetType)
    {
        if (value == null || value == DBNull.Value)
            return GetDefaultValue(targetType);

        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        return actualType switch
        {
            // Handle SQL Server specific conversions
            Type t when t == typeof(DateTime) && value is SqlDateTime sqlDateTime => 
                sqlDateTime.Value,
            
            Type t when t == typeof(string) && value is SqlString sqlString => 
                sqlString.IsNull ? null : sqlString.Value,
            
            Type t when t == typeof(byte[]) && value is SqlBinary sqlBinary => 
                sqlBinary.IsNull ? null : sqlBinary.Value,
            
            Type t when t == typeof(Guid) && value is SqlGuid sqlGuid => 
                sqlGuid.IsNull ? Guid.Empty : sqlGuid.Value,
            
            Type t when t == typeof(decimal) && value is SqlDecimal sqlDecimal => 
                sqlDecimal.IsNull ? 0m : sqlDecimal.Value,
            
            Type t when t == typeof(bool) && value is SqlBoolean sqlBoolean => 
                sqlBoolean.IsNull ? false : sqlBoolean.Value,
            
            // Handle JSON deserialization for complex objects
            Type t when !IsSimpleType(t) && value is string jsonString => 
                JsonSerializer.Deserialize(jsonString, targetType),
            
            // Handle spatial types
            Type t when t.Name == "SqlGeography" => value,
            Type t when t.Name == "SqlGeometry" => value,
            Type t when t.Name == "SqlHierarchyId" => value,
            
            // Handle standard type conversions
            Type t when t == typeof(byte) => Convert.ToByte(value),
            Type t when t == typeof(short) => Convert.ToInt16(value),
            Type t when t == typeof(int) => Convert.ToInt32(value),
            Type t when t == typeof(long) => Convert.ToInt64(value),
            Type t when t == typeof(float) => Convert.ToSingle(value),
            Type t when t == typeof(double) => Convert.ToDouble(value),
            Type t when t == typeof(decimal) => Convert.ToDecimal(value),
            Type t when t == typeof(bool) => Convert.ToBoolean(value),
            Type t when t == typeof(DateTime) => Convert.ToDateTime(value),
            Type t when t == typeof(DateTimeOffset) => value is DateTimeOffset dto ? dto : DateTimeOffset.Parse(value.ToString()!),
            Type t when t == typeof(TimeSpan) => value is TimeSpan ts ? ts : TimeSpan.Parse(value.ToString()!),
            Type t when t == typeof(Guid) => value is Guid guid ? guid : Guid.Parse(value.ToString()!),
            Type t when t == typeof(string) => value.ToString(),
            Type t when t == typeof(char) => Convert.ToChar(value),
            
            // Default case - try direct cast
            _ => Convert.ChangeType(value, actualType)
        };
    }

    /// <inheritdoc />
    public bool SupportsType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        // Check if it's in our type mappings
        if (TypeMappings.ContainsKey(type) || TypeMappings.ContainsKey(actualType))
            return true;

        // Check for SQL Server specific types
        if (actualType.Name is "SqlGeography" or "SqlGeometry" or "SqlHierarchyId")
            return true;

        // Check for .NET 6+ date/time types
        if (actualType == typeof(DateOnly) || actualType == typeof(TimeOnly))
            return true;

        // Support complex types through JSON serialization
        return !IsSimpleType(actualType) || actualType == typeof(byte[]);
    }

    /// <inheritdoc />
    public object? GetDefaultValue(Type type)
    {
        if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            return Activator.CreateInstance(type);
        
        return null;
    }

    /// <inheritdoc />
    public string GetDatabaseTypeName(Type dotNetType, int? length = null, int? precision = null, int? scale = null)
    {
        var actualType = Nullable.GetUnderlyingType(dotNetType) ?? dotNetType;

        // Handle special cases with parameters
        return actualType switch
        {
            Type t when t == typeof(string) => length.HasValue 
                ? length.Value == -1 
                    ? "NVARCHAR(MAX)" 
                    : $"NVARCHAR({length})"
                : "NVARCHAR(255)",
                
            Type t when t == typeof(decimal) => precision.HasValue && scale.HasValue 
                ? $"DECIMAL({precision},{scale})" 
                : precision.HasValue 
                    ? $"DECIMAL({precision},0)" 
                    : "DECIMAL(18,2)",
                    
            Type t when t == typeof(byte[]) => length.HasValue 
                ? length.Value == -1 
                    ? "VARBINARY(MAX)" 
                    : $"VARBINARY({length})"
                : "VARBINARY(MAX)",
                
            // .NET 6+ date/time types
            Type t when t == typeof(DateOnly) => "DATE",
            Type t when t == typeof(TimeOnly) => "TIME",
            
            // SQL Server specific types
            Type t when t.Name == "SqlGeography" => "GEOGRAPHY",
            Type t when t.Name == "SqlGeometry" => "GEOMETRY",
            Type t when t.Name == "SqlHierarchyId" => "HIERARCHYID",
            
            // Use the mapping or default
            _ => TypeMappings.TryGetValue(dotNetType, out var mapping) 
                ? mapping 
                : TypeMappings.TryGetValue(actualType, out var actualMapping) 
                    ? actualMapping 
                    : "NVARCHAR(MAX)" // Default for complex types (JSON)
        };
    }

    /// <inheritdoc />
    public bool RequiresSpecialNullHandling(object? value, Type type)
    {
        // SQL Server requires special handling for certain types
        var actualType = Nullable.GetUnderlyingType(type) ?? type;

        return actualType switch
        {
            Type t when t == typeof(DateTime) => value is DateTime dt && dt == DateTime.MinValue,
            Type t when t == typeof(string) => value is string str && string.IsNullOrEmpty(str),
            Type t when t.Name.StartsWith("Sql") => true, // SQL Server specific types
            _ => false
        };
    }

    /// <summary>
    /// Converts a .NET object to a SQL Server table-valued parameter compatible format.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="sqlServerType">The SQL Server type name.</param>
    /// <returns>The converted value for table-valued parameters.</returns>
    public object? ConvertForTableValuedParameter(object? value, string sqlServerType)
    {
        if (value == null)
            return DBNull.Value;

        return sqlServerType.ToUpperInvariant() switch
        {
            "DATETIME2" when value is DateTime dt => dt,
            "DATETIMEOFFSET" when value is DateTimeOffset dto => dto,
            "TIME" when value is TimeSpan ts => ts,
            "DATE" when value is DateTime dt => dt.Date,
            "BIT" when value is bool b => b,
            "UNIQUEIDENTIFIER" when value is Guid g => g,
            "GEOGRAPHY" => value, // Spatial types pass through
            "GEOMETRY" => value,
            "HIERARCHYID" => value,
            _ => value
        };
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive 
            || type.IsEnum 
            || type == typeof(string) 
            || type == typeof(decimal) 
            || type == typeof(DateTime) 
            || type == typeof(DateTimeOffset) 
            || type == typeof(TimeSpan) 
            || type == typeof(Guid) 
            || type == typeof(DateOnly) 
            || type == typeof(TimeOnly);
    }
}