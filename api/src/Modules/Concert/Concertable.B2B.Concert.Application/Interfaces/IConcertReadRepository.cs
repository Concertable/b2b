using Concertable.B2B.Concert.Application.DTOs;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertReadRepository
{
    Task<PublishedConcert?> GetPublishedByIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<int>> GetEndedPendingCompletionIdsAsync(
        DateTime endedBeforeUtc, int take, CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetUpcomingByVenueIdAsync(int venueId, CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetUpcomingByArtistIdAsync(int artistId, CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetHistoryByVenueIdAsync(int venueId, CancellationToken ct = default);
    Task<IReadOnlyList<PublishedConcert>> GetHistoryByArtistIdAsync(int artistId, CancellationToken ct = default);
}
