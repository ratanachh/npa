using NPA.Generators.Models;

namespace NPA.Generators.Comparers;

/// <summary>
/// Equality comparer for RepositoryInfo to enable incremental generator caching.
/// Only regenerates code when repository metadata actually changes.
/// </summary>
internal class RepositoryInfoComparer : IEqualityComparer<RepositoryInfo>
{
    public bool Equals(RepositoryInfo? x, RepositoryInfo? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        // Compare basic properties
        if (x.InterfaceName != y.InterfaceName ||
            x.FullInterfaceName != y.FullInterfaceName ||
            x.Namespace != y.Namespace ||
            x.EntityType != y.EntityType ||
            x.KeyType != y.KeyType ||
            x.HasCompositeKey != y.HasCompositeKey)
            return false;

        // Compare composite key properties
        if (!x.CompositeKeyProperties.SequenceEqual(y.CompositeKeyProperties))
            return false;

        // Compare methods
        if (x.Methods.Count != y.Methods.Count)
            return false;

        for (int i = 0; i < x.Methods.Count; i++)
        {
            if (!MethodInfoEquals(x.Methods[i], y.Methods[i]))
                return false;
        }

        // Compare many-to-many relationships
        if (x.ManyToManyRelationships.Count != y.ManyToManyRelationships.Count)
            return false;

        for (int i = 0; i < x.ManyToManyRelationships.Count; i++)
        {
            if (!ManyToManyRelationshipInfoEquals(x.ManyToManyRelationships[i], y.ManyToManyRelationships[i]))
                return false;
        }

        // Compare multi-tenancy information
        if (!MultiTenantInfoEquals(x.MultiTenantInfo, y.MultiTenantInfo))
            return false;

        return true;
    }

    public int GetHashCode(RepositoryInfo obj)
    {
        if (obj is null) return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (obj.InterfaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.FullInterfaceName?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.Namespace?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.EntityType?.GetHashCode() ?? 0);
            hash = hash * 31 + (obj.KeyType?.GetHashCode() ?? 0);
            hash = hash * 31 + obj.HasCompositeKey.GetHashCode();

            foreach (var prop in obj.CompositeKeyProperties)
                hash = hash * 31 + (prop?.GetHashCode() ?? 0);

            foreach (var method in obj.Methods)
                hash = hash * 31 + GetMethodInfoHashCode(method);

            foreach (var rel in obj.ManyToManyRelationships)
                hash = hash * 31 + GetManyToManyHashCode(rel);

            if (obj.MultiTenantInfo != null)
                hash = hash * 31 + GetMultiTenantHashCode(obj.MultiTenantInfo);

            return hash;
        }
    }

    private bool MethodInfoEquals(MethodInfo x, MethodInfo y)
    {
        if (x.Name != y.Name || x.ReturnType != y.ReturnType)
            return false;

        if (x.Parameters.Count != y.Parameters.Count)
            return false;

        for (int i = 0; i < x.Parameters.Count; i++)
        {
            if (x.Parameters[i].Name != y.Parameters[i].Name ||
                x.Parameters[i].Type != y.Parameters[i].Type)
                return false;
        }

        return MethodAttributeInfoEquals(x.Attributes, y.Attributes);
    }

    private bool MethodAttributeInfoEquals(MethodAttributeInfo x, MethodAttributeInfo y)
    {
        return x.HasQuery == y.HasQuery &&
               x.QuerySql == y.QuerySql &&
               x.NativeQuery == y.NativeQuery &&
               x.HasStoredProcedure == y.HasStoredProcedure &&
               x.ProcedureName == y.ProcedureName &&
               x.Schema == y.Schema &&
               x.HasMultiMapping == y.HasMultiMapping &&
               x.KeyProperty == y.KeyProperty &&
               x.SplitOn == y.SplitOn &&
               x.HasBulkOperation == y.HasBulkOperation &&
               x.BatchSize == y.BatchSize &&
               x.UseTransaction == y.UseTransaction &&
               x.CommandTimeout == y.CommandTimeout &&
               x.Buffered == y.Buffered;
    }

    private bool ManyToManyRelationshipInfoEquals(ManyToManyRelationshipInfo x, ManyToManyRelationshipInfo y)
    {
        return x.PropertyName == y.PropertyName &&
               x.PropertyType == y.PropertyType &&
               x.CollectionElementType == y.CollectionElementType &&
               x.JoinTableName == y.JoinTableName &&
               x.JoinTableSchema == y.JoinTableSchema &&
               x.JoinColumns.SequenceEqual(y.JoinColumns) &&
               x.InverseJoinColumns.SequenceEqual(y.InverseJoinColumns) &&
               x.MappedBy == y.MappedBy;
    }

    private bool MultiTenantInfoEquals(MultiTenantInfo? x, MultiTenantInfo? y)
    {
        if (x is null && y is null) return true;
        if (x is null || y is null) return false;

        return x.IsMultiTenant == y.IsMultiTenant &&
               x.TenantIdProperty == y.TenantIdProperty &&
               x.EnforceTenantIsolation == y.EnforceTenantIsolation &&
               x.AllowCrossTenantQueries == y.AllowCrossTenantQueries;
    }

    private int GetMethodInfoHashCode(MethodInfo method)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (method.Name?.GetHashCode() ?? 0);
            hash = hash * 31 + (method.ReturnType?.GetHashCode() ?? 0);
            foreach (var param in method.Parameters)
            {
                hash = hash * 31 + (param.Name?.GetHashCode() ?? 0);
                hash = hash * 31 + (param.Type?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }

    private int GetManyToManyHashCode(ManyToManyRelationshipInfo rel)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (rel.PropertyName?.GetHashCode() ?? 0);
            hash = hash * 31 + (rel.CollectionElementType?.GetHashCode() ?? 0);
            hash = hash * 31 + (rel.JoinTableName?.GetHashCode() ?? 0);
            return hash;
        }
    }

    private int GetMultiTenantHashCode(MultiTenantInfo info)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + info.IsMultiTenant.GetHashCode();
            hash = hash * 31 + (info.TenantIdProperty?.GetHashCode() ?? 0);
            hash = hash * 31 + info.EnforceTenantIsolation.GetHashCode();
            hash = hash * 31 + info.AllowCrossTenantQueries.GetHashCode();
            return hash;
        }
    }
}

