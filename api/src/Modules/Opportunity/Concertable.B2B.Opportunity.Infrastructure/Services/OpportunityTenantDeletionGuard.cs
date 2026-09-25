using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Services;

internal sealed class OpportunityTenantDeletionGuard(
    OpportunityPrivilegedDbContext context,
    CommandTransactionAccessor transactions) : ITenantDeletionGuard
{
    public async Task<bool> HasLiveObligationsAsync(Guid tenantId, CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Tenant deletion requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        return await context.Opportunities.AnyAsync(opportunity =>
            opportunity.TenantId == tenantId && opportunity.State != OpportunityState.Withdrawn,
            ct);
    }
}
