using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.Opportunity.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Repositories;

internal sealed class OpportunityRepository : TenantScopedRepository<OpportunityEntity>, IOpportunityRepository
{
    private readonly OpportunityDbContext context;
    private readonly TimeProvider timeProvider;

    public OpportunityRepository(OpportunityDbContext context, ITenantContext tenant, TimeProvider timeProvider)
        : base(context, tenant)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await context.Opportunities
            .ActiveForVenue(venueId, timeProvider.GetUtcNow())
            .ToListAsync();

    public Task<int?> GetDealIdByIdAsync(int opportunityId) =>
        context.Opportunities
            .Where(o => o.Id == opportunityId)
            .Select(o => (int?)o.DealId)
            .FirstOrDefaultAsync();

    public async Task<bool> TryFillAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default) =>
        await context.Opportunities
            .Where(opportunity =>
                opportunity.Id == opportunityId &&
                opportunity.TenantId == venueTenantId &&
                opportunity.State == OpportunityState.Open)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    opportunity => opportunity.State,
                    OpportunityState.Filled),
                ct) == 1;

    public async Task<IReadOnlyList<OpportunityEntity>> GetByIdsAsync(IReadOnlyCollection<int> ids) =>
        await context.Opportunities
            .Where(o => ids.Contains(o.Id))
            .ToListAsync();

    public async Task<IReadOnlyList<OpportunityEntity>> GetOpenByVenueTenantIdAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        await context.Opportunities
            .AsNoTracking()
            .Where(opportunity => opportunity.TenantId == venueTenantId)
            .WhereActive(timeProvider.GetUtcNow())
            .OrderBy(opportunity => opportunity.Period.Start)
            .Take(5)
            .ToListAsync(ct);

}
