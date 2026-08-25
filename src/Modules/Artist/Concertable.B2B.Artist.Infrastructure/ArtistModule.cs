using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure;

internal sealed class ArtistModule : IArtistModule
{
    private readonly IArtistService artistService;
    private readonly IArtistReviewService reviewService;

    public ArtistModule(
        IArtistService artistService,
        IArtistReviewService reviewService)
    {
        this.artistService = artistService;
        this.reviewService = reviewService;
    }

    public Task<Option<ArtistSummary>> GetSummaryAsync(
        int artistId,
        CancellationToken ct = default) =>
        artistService.GetSummaryAsync(artistId, ct);

    public Task<IReadOnlyList<ArtistSummary>> GetSummariesAsync(
        IReadOnlyCollection<int> artistIds,
        CancellationToken ct = default) =>
        artistService.GetSummariesAsync(artistIds, ct);

    public Task<IReadOnlySet<Genre>> GetGenresAsync(
        int artistId,
        CancellationToken ct = default) =>
        artistService.GetGenresAsync(artistId, ct);

    public Task<Option<ArtistProfile>> GetProfileAsync(
        int artistId,
        CancellationToken ct = default) =>
        artistService.GetProfileAsync(artistId, ct);

    public Task<Option<ArtistProfile>> GetCurrentProfileAsync(CancellationToken ct = default) =>
        artistService.GetCurrentProfileAsync(ct);

    public Task<ReviewSummary> GetReviewSummaryAsync(
        int artistId,
        CancellationToken ct = default) =>
        reviewService.GetSummaryAsync(artistId, ct);
}
