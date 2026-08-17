using Reunion;

namespace Concertable.B2B.Venue.Contracts;

public interface IVenueModule
{
    Task<Option<VenueSummary>> GetSummaryAsync(int venueId, CancellationToken ct = default);
}
