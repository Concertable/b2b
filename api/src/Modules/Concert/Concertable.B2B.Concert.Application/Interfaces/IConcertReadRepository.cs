using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The public marketplace surface over concerts — the details page and venue/artist page listings.
/// Reads run with the "Tenant" filter lifted: the concert row is public, but these queries identify
/// concerts THROUGH their (party-filtered) booking chain, so unlifted they vanish for non-parties.
/// Party/host reads live on <see cref="IConcertRepository"/>; availability booleans on
/// <see cref="IConcertAvailability"/>.
/// </summary>
internal interface IConcertReadRepository
{
    Task<ConcertDetails?> GetDetailsByIdAsync(int id);

    /// <summary>The public listing's own shape. Publication is the predicate, so an unpublished draft cannot
    /// be reached by guessing its id, and nothing private is in the projection to leak if it were.</summary>
    Task<PublishedConcert?> GetPublishedByIdAsync(int id, CancellationToken ct = default);

    /// <summary>Concerts whose engagement has ended and whose settlement has not yet run, oldest first.</summary>
    Task<IReadOnlyList<int>> GetEndedPendingCompletionIdsAsync(
        DateTime endedBeforeUtc, int take, CancellationToken ct = default);
    Task<ConcertSummary?> GetSummaryAsync(int id);
    Task<IEnumerable<ConcertSummary>> GetUpcomingByVenueIdAsync(int venueId);
    Task<IEnumerable<ConcertSummary>> GetUpcomingByArtistIdAsync(int artistId);
    Task<IEnumerable<ConcertSummary>> GetHistoryByVenueIdAsync(int venueId);
    Task<IEnumerable<ConcertSummary>> GetHistoryByArtistIdAsync(int artistId);
}
