using Concertable.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Errors;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Mappers;

namespace Concertable.B2B.Venue.Infrastructure.Services;

internal sealed class VenueReviewService : IVenueReviewService
{
    private readonly IVenueService venueService;
    private readonly IVenueReviewRepository reviewRepository;

    public VenueReviewService(
        IVenueService venueService,
        IVenueReviewRepository reviewRepository)
    {
        this.venueService = venueService;
        this.reviewRepository = reviewRepository;
    }

    public async Task<ReviewSummary> GetSummaryAsync(int venueId, CancellationToken ct = default) =>
        (await reviewRepository.GetRatingByVenueIdAsync(venueId, ct)).ToReviewSummary();

    public async Task<IPagination<ReviewDto>> GetPagedAsync(int venueId, IPageParams pageParams) =>
        (await reviewRepository.GetPagedByVenueIdAsync(venueId, pageParams)).Select(review => review.ToReviewDto());

    public async Task<Result<IReadOnlyList<VenueReview>, VenueError>> GetRecentForCurrentAsync(
        int take,
        CancellationToken ct = default)
    {
        var venue = await venueService.GetIdForCurrentTenantAsync();
        if (!venue.TryGetValue(out var venueId))
            return new VenueError.CurrentTenantNotFound();

        return new Success<IReadOnlyList<VenueReview>>(
            await reviewRepository.GetRecentByVenueIdAsync(venueId, take, ct));
    }
}
