namespace Concertable.B2B.Tenant.Contracts;

/// <summary>Requires the active tenant to have the specified type for permission-protected endpoints.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RequiredTenantTypeAttribute : Attribute
{
    public RequiredTenantTypeAttribute(TenantType tenantType) => TenantType = tenantType;

    public TenantType TenantType { get; }
}
