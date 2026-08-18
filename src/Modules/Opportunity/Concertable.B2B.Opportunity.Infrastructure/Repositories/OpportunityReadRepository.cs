using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.Opportunity.Infrastructure.Extensions;
using Concertable.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Repositories;

internal sealed class OpportunityReadRepository : IOpportunityReadRepository
{
    private readonly IOpportunityReadDbContext context;
    private readonly TimeProvider timeProvider;

    public OpportunityReadRepository(IOpportunityReadDbContext context, TimeProvider timeProvider)
    {
        this.context = context;
        this.timeProvider = timeProvider;
    }

    public async Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId, IPageParams pageParams) =>
        await ActiveForVenue(venueId).ToPaginationAsync(pageParams);

    public async Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId) =>
        await ActiveForVenue(venueId).ToListAsync();

    public async Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default)
    {
        if (opportunityIds.Count == 0)
            return new HashSet<int>();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var ids = await context.Opportunities
            .Where(opportunity =>
                opportunityIds.Contains(opportunity.Id) &&
                opportunity.Period.End > now)
            .Select(opportunity => opportunity.Id)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return context.Opportunities.CountAsync(
            opportunity =>
                opportunity.TenantId == venueTenantId &&
                opportunity.State == OpportunityState.Open &&
                opportunity.Period.End > now,
            ct);
    }

    private IQueryable<OpportunityEntity> ActiveForVenue(int venueId) =>
        context.Opportunities.ActiveForVenue(venueId, timeProvider.GetUtcNow());
}
