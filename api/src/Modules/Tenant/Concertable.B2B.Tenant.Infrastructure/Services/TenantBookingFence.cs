using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantBookingFence(ITenantRepository repository) : ITenantBookingFence
{
    public async Task RequireAsync(
        Guid venueTenantId,
        Guid artistTenantId,
        CancellationToken ct = default)
    {
        foreach (var tenantId in new[] { venueTenantId, artistTenantId }.Distinct().Order())
        {
            if (await repository.GetByIdForAdministrationAsync(tenantId, ct) is null)
                throw new InvalidOperationException($"Booking tenant {tenantId} no longer exists.");
        }
    }
}
