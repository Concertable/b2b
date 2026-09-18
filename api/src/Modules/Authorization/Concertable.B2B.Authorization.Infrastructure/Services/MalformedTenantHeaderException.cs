namespace Concertable.B2B.Authorization.Infrastructure.Services;

public sealed class MalformedTenantHeaderException()
    : InvalidOperationException($"'{TenantHeaders.TenantId}' is present but is not a tenant id.");
