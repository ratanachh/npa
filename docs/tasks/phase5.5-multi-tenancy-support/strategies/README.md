# Multi-Tenancy Strategy Samples

This directory contains sample implementations for different multi-tenancy isolation strategies.

## 📚 Essential Reading

**🔥 START HERE:** [MULTITENANT_ATTRIBUTE_GUIDE.md](MULTITENANT_ATTRIBUTE_GUIDE.md)
- When to use `[MultiTenant]` attribute ✅
- When NOT to use `[MultiTenant]` attribute ❌
- Entity definition for each strategy with code examples
- Common mistakes and how to avoid them
- Migration paths between strategies

## Strategies Supported

### 1. Discriminator Column (Default - ✅ Implemented)
- **[MultiTenant] Attribute**: ✅ **YES** - Required for automatic filtering
- **TenantId Property**: ✅ **YES** - Must exist in entity
- **TenantId Column**: ✅ **YES** - Column in database table
- **Isolation**: Row-level filtering using `WHERE TenantId = 'tenant1'`
- **Database**: Single database, single schema
- **SQL Example**: `SELECT * FROM Products WHERE TenantId = 'tenant1'`
- **Pros**: Simple, cost-effective, easy to manage
- **Cons**: Risk of cross-tenant data leaks if not careful, complex queries
- **Best For**: Small to medium SaaS applications

### 2. Database Per Tenant (Sample Provided)
- **[MultiTenant] Attribute**: ❌ **NO** - Don't use this attribute
- **TenantId Property**: ❌ **NO** - Not needed
- **TenantId Column**: ❌ **NO** - No column in database
- **Isolation**: Separate database for each tenant
- **Database**: Multiple databases
- **SQL Example**: `SELECT * FROM Products` (connection to `TenantDB_tenant1`)
- **Pros**: Strong isolation, independent backups, custom schema per tenant
- **Cons**: Higher cost, complex migrations, connection pool management
- **Best For**: Enterprise customers, compliance requirements

### 3. Schema Per Tenant (Sample Provided)
- **[MultiTenant] Attribute**: ❌ **NO** - Don't use this attribute
- **TenantId Property**: ❌ **NO** - Not needed
- **TenantId Column**: ❌ **NO** - No column in database
- **Isolation**: Separate schema for each tenant within same database
- **Database**: Single database, multiple schemas
- **SQL Example**: `SELECT * FROM tenant1.Products`
- **Pros**: Good isolation, shared resources, easier than DB-per-tenant
- **Cons**: Schema-level permissions needed, migration complexity
- **Best For**: Medium to large SaaS with moderate isolation needs

## Sample Implementations

- **DiscriminatorColumn/**: Default implementation (already in main codebase)
- **DatabasePerTenant/**: Shows how to switch connections based on tenant
- **SchemaPerTenant/**: Shows how to modify table names with schema prefix

## Quick Comparison

| Feature | Discriminator | Database Per Tenant | Schema Per Tenant |
|---------|--------------|---------------------|-------------------|
| Isolation | Row-level | Database-level | Schema-level |
| Cost | Low | High | Medium |
| Complexity | Low | High | Medium |
| Performance | Good | Excellent | Very Good |
| Scalability | Limited | Excellent | Good |
| Migrations | Simple | Complex | Moderate |
| Backup/Restore | Complex | Simple | Moderate |
