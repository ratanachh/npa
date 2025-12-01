namespace NPA.Generators;

internal class MultiTenantInfo
{
    public bool IsMultiTenant { get; set; }
    public string TenantIdProperty { get; set; } = "TenantId";
    public bool EnforceTenantIsolation { get; set; } = true;
    public bool AllowCrossTenantQueries { get; set; } = false;
}