using Concertable.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Services;

internal sealed class VenueReviewService : IVenueReviewService
{
    private readonly VenueDbContext context;
    private readonly IVenueService venueService;

    public VenueReviewService(VenueDbContext context, IVenueService venueService)
    {
        this.context = context;
        this.venueService = venueService;
    }

    public async Task<ReviewSummary> GetSummaryAsync(int venueId)
    {
        var projection = await context.VenueRatingProjections
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.VenueId == venueId);
        return projection.ToReviewSummary();
    }

    public Task<IPagination<ReviewDto>> GetPagedAsync(int venueId, IPageParams pageParams) =>
        context.VenueReviews
            .AsNoTracking()
            .Where(r => r.VenueId == venueId)
            .OrderByDescending(r => r.Id)
            .Select(r => new ReviewDto { Id = r.Id, Email = r.Email, Stars = (int)r.Stars, Details = r.Details })
            .ToPaginationAsync(pageParams);

    public async Task<IReadOnlyList<RecentReviewDto>> GetRecentForCurrentAsync(int take)
    {
        var venueId = await venueService.GetIdForCurrentUserAsync();

        return await context.VenueReviews
            .AsNoTracking()
            .Where(r => r.VenueId == venueId)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Take(take)
            .Select(r => new RecentReviewDto(
                r.Id,
                r.Email,
                null,
                (int)r.Stars,
                r.Details,
                r.CreatedAt,
                $"/_venue/find/venue/{venueId}"))
            .ToListAsync();
    }
}
