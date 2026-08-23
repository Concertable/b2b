using Reunion;

namespace Concertable.B2B.Venue.Contracts;

public interface IVenueModule
{
    Task<Option<VenueSummary>> GetSummaryAsync(int venueId, CancellationToken ct = default);
    Task<Option<int>> GetCurrentIdAsync(CancellationToken ct = default);
    Task<Option<VenueProfile>> GetProfileAsync(int venueId, CancellationToken ct = default);
    Task<IReadOnlyList<VenueProfile>> GetProfilesAsync(
        IReadOnlyCollection<int> venueIds,
        CancellationToken ct = default);
    Task<Option<VenueProfile>> GetCurrentProfileAsync(CancellationToken ct = default);
}
