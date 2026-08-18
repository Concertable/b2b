using Concertable.B2B.Opportunity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Repositories;

internal sealed class OpportunityHandoffRepository(OpportunityHandoffDbContext context)
    : Repository<OpportunityEntity, int>(context), IOpportunityHandoffRepository
{
    public async Task<bool> TryClaimAsync(
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
}
