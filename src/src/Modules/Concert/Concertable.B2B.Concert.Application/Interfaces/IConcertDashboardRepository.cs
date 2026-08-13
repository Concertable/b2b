using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDashboardRepository
{
    Task<VenueDashboardCounts?> GetVenueCountsAsync(int venueId, CancellationToken ct = default);

    Task<ArtistDashboardCounts?> GetArtistCountsAsync(
        int artistId,
        IReadOnlyCollection<DealType> checkoutCapableDealTypes,
        CancellationToken ct = default);
}
