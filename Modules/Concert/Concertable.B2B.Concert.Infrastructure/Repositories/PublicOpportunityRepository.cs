using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

/* The open-check is an EXISTS over application states and must see ALL parties' applications,
   or booked opportunities re-appear as open to third parties. No application contents are returned. */
internal sealed class PublicOpportunityRepository(PublicConcertDbContext context, TimeProvider timeProvider)
    : IPublicOpportunityRepository
{
    public async Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams)
    {
        var query = context.Opportunities
            .Where(o => o.VenueId == venueId)
            .WhereActive(timeProvider.GetUtcNow())
            .OrderBy(o => o.Period.Start);

        return await query.ToPaginationAsync(pageParams);
    }

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await context.Opportunities
            .Where(o => o.VenueId == venueId)
            .WhereActive(timeProvider.GetUtcNow())
            .OrderBy(o => o.Period.Start)
            .ToListAsync();
}
