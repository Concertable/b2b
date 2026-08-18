using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationDashboardService
{
    Task<int> GetVenuePendingCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<ArtistApplicationDashboardCounts> GetArtistCountsAsync(
        Guid artistTenantId,
        IReadOnlySet<DealType> acceptCheckoutDealTypes,
        CancellationToken ct = default);
}
