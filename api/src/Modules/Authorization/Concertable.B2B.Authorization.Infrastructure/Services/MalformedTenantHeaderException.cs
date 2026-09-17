namespace Concertable.B2B.Authorization.Infrastructure.Services;

/// <summary>
/// An explicit <c>X-Tenant-Id</c> that is not a tenant id. Selecting a different tenant would answer a
/// question the caller did not ask, so the request fails rather than falls back.
/// </summary>
public sealed class MalformedTenantHeaderException()
    : InvalidOperationException($"'{TenantHeaders.TenantId}' is present but is not a tenant id.");
