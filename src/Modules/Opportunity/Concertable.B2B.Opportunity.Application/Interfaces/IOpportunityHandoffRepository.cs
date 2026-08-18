using Concertable.DataAccess.Application;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityHandoffRepository : IRepository<OpportunityEntity>
{
    Task<bool> TryClaimAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default);
}
