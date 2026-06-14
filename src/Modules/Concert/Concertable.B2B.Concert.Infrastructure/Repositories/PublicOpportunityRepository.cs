using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class PublicOpportunityRepository : OpportunityRepository<PublicConcertDbContext>, IPublicOpportunityRepository
{
    public PublicOpportunityRepository(PublicConcertDbContext context, ITenantContext tenant, TimeProvider timeProvider)
        : base(context, tenant, timeProvider) { }

    public async Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams) =>
        await ActiveForVenue(venueId).ToPaginationAsync(pageParams);
}
