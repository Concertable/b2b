using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class OpportunityRepository : TenantScopedRepository<OpportunityEntity>, IOpportunityRepository
{
    private readonly TimeProvider timeProvider;

    public OpportunityRepository(ConcertDbContext context, ITenantContext tenant, TimeProvider timeProvider)
        : base(context, tenant)
    {
        this.timeProvider = timeProvider;
    }

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await context.Opportunities
            .Where(o => o.VenueId == venueId)
            .WhereActive(timeProvider.GetUtcNow())
            .ToListAsync();

    public async Task<Guid?> GetOwnerByIdAsync(int opportunityId) =>
        await context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (Guid?)o.Venue.UserId)
            .FirstOrDefaultAsync();

    public Task<int?> GetContractIdByIdAsync(int opportunityId) =>
        context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (int?)o.ContractId)
            .FirstOrDefaultAsync();

    public async Task<OpportunityEntity?> GetWithVenueByIdAsync(int id) =>
        await context.Opportunities
            .Where(o => o.Id == id)
            .Include(o => o.Venue)
            .FirstOrDefaultAsync();

    public async Task<OpportunityEntity?> GetByApplicationIdAsync(int id) =>
        await context.Opportunities
            .Include(o => o.Venue)
            .Where(o => o.Applications.Any(a => a.Id == id))
            .FirstOrDefaultAsync();

    public async Task<(string Name, Guid UserId)?> GetVenueSummaryByIdAsync(int opportunityId)
    {
        var venue = await context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => new { o.Venue.Name, o.Venue.UserId })
            .FirstOrDefaultAsync();
        return venue is null ? null : (venue.Name, venue.UserId);
    }
}
