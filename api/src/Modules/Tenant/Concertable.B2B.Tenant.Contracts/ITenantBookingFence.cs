namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantBookingFence
{
    Task RequireAsync(Guid venueTenantId, Guid artistTenantId, CancellationToken ct = default);
}
