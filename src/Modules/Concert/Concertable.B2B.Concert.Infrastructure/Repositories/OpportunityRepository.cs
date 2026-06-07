using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class OpportunityRepository : Repository<OpportunityEntity>, IOpportunityRepository
{
    private readonly TimeProvider timeProvider;

    public OpportunityRepository(ConcertDbContext context, TimeProvider timeProvider) : base(context)
    {
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int id, IPageParams pageParams)
    {
        var query = context.Opportunities
            .Where(o => o.VenueId == id)
            .WhereActive(timeProvider.GetUtcNow())
            .OrderBy(o => o.Period.Start);

        return await query.ToPaginationAsync(pageParams);
    }

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId)
    {
        return await context.Opportunities
            .Where(o => o.VenueId == venueId)
            .WhereActive(timeProvider.GetUtcNow())
            .OrderBy(o => o.Period.Start)
            .ToListAsync();
    }

    public async Task<Guid?> GetOwnerByIdAsync(int opportunityId)
    {
        return await context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (Guid?)o.Venue.UserId)
            .FirstOrDefaultAsync();
    }

    public Task<int?> GetContractIdByIdAsync(int opportunityId)
    {
        return context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (int?)o.ContractId)
            .FirstOrDefaultAsync();
    }

    public async Task<OpportunityEntity?> GetWithVenueByIdAsync(int id)
    {
        return await context.Opportunities
            .Where(o => o.Id == id)
            .Include(o => o.Venue)
            .FirstOrDefaultAsync();
    }

    public async Task<OpportunityEntity?> GetByApplicationIdAsync(int id)
    {
        return await context.Opportunities
            .Include(o => o.Venue)
            .Where(o => o.Applications.Any(a => a.Id == id))
            .FirstOrDefaultAsync();
    }

}
