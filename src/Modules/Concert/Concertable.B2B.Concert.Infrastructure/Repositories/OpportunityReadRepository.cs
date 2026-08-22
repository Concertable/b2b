using Concertable.B2B.Concert.Application.Projections;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class OpportunityReadRepository : IOpportunityReadRepository
{
    private readonly IConcertReadDbContext context;
    private readonly TimeProvider timeProvider;

    public OpportunityReadRepository(IConcertReadDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams) =>
        await ActiveForVenue(venueId).ToPaginationAsync(pageParams);

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await ActiveForVenue(venueId).ToListAsync();

    public async Task<IReadOnlyList<OpportunityMatchProjection>> GetMatchCandidatesAsync(
        int artistId,
        IReadOnlySet<Genre> genres)
    {
        var genreList = genres.ToList();
        var now = timeProvider.GetUtcNow();
        return await context.Opportunities
            .Include(o => o.Venue)
            .WhereActive(now)
            .Where(o => !o.Applications.Any(a => a.ArtistId == artistId))
            .Where(o => o.Genres.Count == 0 || o.Genres.Any(g => genreList.Contains(g)))
            .OrderBy(o => o.Period.Start)
            .Take(5)
            .Select(o => new OpportunityMatchProjection
            {
                Id = o.Id,
                VenueId = o.VenueId,
                VenueName = o.Venue.Name,
                County = o.Venue.Address.County,
                Town = o.Venue.Address.Town,
                StartDate = o.Period.Start,
                EndDate = o.Period.End,
                Genres = o.Genres,
                DealId = o.DealId
            })
            .ToListAsync();
    }

    private IQueryable<OpportunityEntity> ActiveForVenue(int venueId) =>
        context.Opportunities
            .Include(o => o.Venue)
            .ActiveForVenue(venueId, timeProvider.GetUtcNow());
}
