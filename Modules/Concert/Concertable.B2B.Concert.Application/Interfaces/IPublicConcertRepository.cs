using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The public marketplace surface over concerts — the details page and venue/artist page listings.
/// Reads run with the "Tenant" filter lifted: the concert row is public, but these queries identify
/// concerts THROUGH their (party-filtered) booking chain, so unlifted they vanish for non-parties.
/// Party/host reads live on <see cref="IConcertRepository"/>; availability booleans on
/// <see cref="IConcertAvailability"/>.
/// </summary>
internal interface IPublicConcertRepository
{
    Task<ConcertDetails?> GetDetailsByIdAsync(int id);
    Task<ConcertSummary?> GetSummaryAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUpcomingByVenueIdAsync(int venueId);
    Task<IEnumerable<ConcertSummary>> GetUpcomingByArtistIdAsync(int artistId);
    Task<IEnumerable<ConcertSummary>> GetHistoryByVenueIdAsync(int venueId);
    Task<IEnumerable<ConcertSummary>> GetHistoryByArtistIdAsync(int artistId);
}
