namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// Transport for the active tenant. Tokens are identity-only, so the acting tenant is request state: the
/// client sends the selected tenant id on this header and <c>TenantContext</c> validates it against the
/// caller's memberships. Absent header + a single membership defaults to it; absent + multi-membership
/// fails closed.
/// </summary>
public static class TenantHeaders
{
    public const string TenantId = "X-Tenant-Id";
}
