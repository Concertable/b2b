using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Errors;
using Concertable.B2B.Artist.Infrastructure.Mappers;
using Concertable.Contracts;

namespace Concertable.B2B.Artist.Infrastructure.Services;

internal sealed class ArtistReviewService : IArtistReviewService
{
    private readonly IArtistService artistService;
    private readonly IArtistReviewRepository reviewRepository;

    public ArtistReviewService(
        IArtistService artistService,
        IArtistReviewRepository reviewRepository)
    {
        this.artistService = artistService;
        this.reviewRepository = reviewRepository;
    }

    public async Task<ReviewSummary> GetSummaryAsync(int artistId, CancellationToken ct = default) =>
        (await reviewRepository.GetRatingByArtistIdAsync(artistId, ct)).ToReviewSummary();

    public async Task<IPagination<ReviewDto>> GetPagedAsync(int artistId, IPageParams pageParams) =>
        (await reviewRepository.GetPagedByArtistIdAsync(artistId, pageParams)).Select(review => review.ToReviewDto());

    public async Task<Result<IReadOnlyList<ArtistReview>, ArtistError>> GetRecentForCurrentAsync(
        int take,
        CancellationToken ct = default)
    {
        var artist = await artistService.GetIdForCurrentTenantAsync();
        if (!artist.TryGetValue(out var artistId))
            return new ArtistError.CurrentTenantNotFound();

        return new Success<IReadOnlyList<ArtistReview>>(
            await reviewRepository.GetRecentByArtistIdAsync(artistId, take, ct));
    }
}
