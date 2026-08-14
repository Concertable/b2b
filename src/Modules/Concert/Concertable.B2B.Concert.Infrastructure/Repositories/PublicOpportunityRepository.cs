using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class PublicOpportunityRepository : OpportunityRepository<PublicConcertDbContext>, IPublicOpportunityRepository
{
    private readonly TimeProvider timeProvider;

    public PublicOpportunityRepository(PublicConcertDbContext context, ITenantContext tenant, TimeProvider timeProvider)
        : base(context, tenant, timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams) =>
        await ActiveForVenue(venueId).ToPaginationAsync(pageParams);

    public async Task<IReadOnlyList<OpportunityListRow>> GetRecommendedAsync(int artistId, IReadOnlySet<Genre> genres)
    {
        var genreList = genres.ToList();
        var now = timeProvider.GetUtcNow();
        return await context.Opportunities
            .AsNoTracking()
            .Include(o => o.Venue)
            .WhereActive(now)
            .Where(o => !o.Applications.Any(a => a.ArtistId == artistId))
            .Where(o => o.Genres.Count == 0 || o.Genres.Any(g => genreList.Contains(g)))
            .OrderBy(o => o.Period.Start)
            .Take(5)
            .Select(o => new OpportunityListRow
            {
                Id = o.Id,
                VenueId = o.VenueId,
                VenueName = o.Venue.Name,
                County = o.Venue.Address.County,
                Town = o.Venue.Address.Town,
                StartDate = o.Period.Start,
                EndDate = o.Period.End,
                Genres = o.Genres,
                DealId = o.DealId,
                ApplicationCount = o.Applications.Count
            })
            .ToListAsync();
    }
}
