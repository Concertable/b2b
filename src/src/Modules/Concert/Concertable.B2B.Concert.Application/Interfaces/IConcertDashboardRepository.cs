using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDashboardRepository
{
    Task<VenueDashboardCounts?> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default);

    Task<ArtistDashboardCounts?> GetArtistCountsAsync(
        Guid artistTenantId,
        IReadOnlyCollection<DealType> checkoutCapableDealTypes,
        CancellationToken ct = default);

    Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default);
}
