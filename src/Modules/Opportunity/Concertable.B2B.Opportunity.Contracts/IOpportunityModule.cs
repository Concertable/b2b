using Reunion;

namespace Concertable.B2B.Opportunity.Contracts;

public interface IOpportunityModule
{
    Task<Option<OpportunityDetails>> GetDetailsAsync(int opportunityId, CancellationToken ct = default);
    Task MarkFilledAsync(int opportunityId, Guid venueTenantId, CancellationToken ct = default);
}
