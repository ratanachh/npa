using NPA.Core.Annotations;

namespace NPA.Core.Tests.MultiTenancy;

/// <summary>
/// Test entity with default multi-tenant configuration.
/// </summary>
[Entity, Table("Products"), MultiTenant]
public class Product
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("Id")]
    public int Id { get; set; }
    
    [Column("TenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [Column("Name")]
    public string Name { get; set; } = string.Empty;
    
    [Column("Price")]
    public decimal Price { get; set; }
}

/// <summary>
/// Test entity with custom tenant property name.
/// </summary>
[Entity, Table("Orders"), MultiTenant(tenantIdProperty: "OrganizationId")]
public class Order
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("Id")]
    public int Id { get; set; }
    
    [Column("OrganizationId")]
    public string OrganizationId { get; set; } = string.Empty;
    
    [Column("OrderNumber")]
    public string OrderNumber { get; set; } = string.Empty;
    
    [Column("Total")]
    public decimal Total { get; set; }
}

/// <summary>
/// Test entity that allows cross-tenant queries.
/// </summary>
[Entity, Table("GlobalSettings"), MultiTenant(AllowCrossTenantQueries = true)]
public class GlobalSetting
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("Id")]
    public int Id { get; set; }
    
    [Column("TenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [Column("Key")]
    public string Key { get; set; } = string.Empty;
    
    [Column("Value")]
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Test entity that doesn't validate tenant on write.
/// </summary>
[Entity, Table("LogEntries"), MultiTenant(ValidateTenantOnWrite = false)]
public class LogEntry
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("Id")]
    public int Id { get; set; }
    
    [Column("TenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [Column("Message")]
    public string Message { get; set; } = string.Empty;
    
    [Column("Timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Test entity that doesn't auto-populate tenant ID.
/// </summary>
[Entity, Table("ManualTenantEntities"), MultiTenant(AutoPopulateTenantId = false)]
public class ManualTenantEntity
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("Id")]
    public int Id { get; set; }
    
    [Column("TenantId")]
    public string TenantId { get; set; } = string.Empty;
    
    [Column("Data")]
    public string Data { get; set; } = string.Empty;
}

/// <summary>
/// Test entity with no multi-tenancy (control entity).
/// </summary>
[Entity, Table("SharedData")]
public class SharedData
{
    [Id]
    [GeneratedValue(GenerationType.Identity)]
    [Column("Id")]
    public int Id { get; set; }
    
    [Column("Value")]
    public string Value { get; set; } = string.Empty;
}
