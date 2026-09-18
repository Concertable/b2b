namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// Transport for the active tenant. Tokens are identity-only, so the acting tenant is request state: the
/// client sends the selected tenant id on this header and the membership context validates it against the
/// caller's memberships. Absent header + a single membership defaults to it; absent + multi-membership
/// fails closed.
/// </summary>
public static class TenantHeaders
{
    public const string TenantId = "X-Tenant-Id";
}
