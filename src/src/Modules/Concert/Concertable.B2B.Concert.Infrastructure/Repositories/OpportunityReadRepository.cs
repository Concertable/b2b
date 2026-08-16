using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class OpportunityReadRepository : IOpportunityReadRepository
{
    private readonly ConcertReadDbContext context;
    private readonly TimeProvider timeProvider;

    public OpportunityReadRepository(ConcertReadDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams) =>
        await ActiveForVenue(venueId).ToPaginationAsync(pageParams);

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await ActiveForVenue(venueId).ToListAsync();

    private IQueryable<OpportunityEntity> ActiveForVenue(int venueId) =>
        context.Opportunities.ActiveForVenue(venueId, timeProvider.GetUtcNow());
}
