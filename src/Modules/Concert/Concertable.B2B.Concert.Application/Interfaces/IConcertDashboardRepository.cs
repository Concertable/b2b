namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertDashboardRepository
{
    Task<VenueConcertDashboardCounts?> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default);

    Task<ArtistConcertDashboardCounts?> GetArtistCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
}

internal sealed record VenueConcertDashboardCounts(
    int UpcomingConcerts,
    int AwaitingDoorRevenue);

internal sealed record ArtistConcertDashboardCounts(int UpcomingConcerts);
