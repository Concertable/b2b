using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.Contracts;

public interface IApplicationModule
{
    bool RequiresApplyCheckout(DealType dealType);
    bool RequiresAcceptCheckout(DealType dealType);
    Task<int> GetVenuePendingCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<ArtistApplicationDashboardCounts> GetArtistDashboardCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
}
