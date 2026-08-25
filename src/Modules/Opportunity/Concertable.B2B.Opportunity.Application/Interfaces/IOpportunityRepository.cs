using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Opportunity.Domain.Entities;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityRepository : ITenantScopedRepository<OpportunityEntity>
{
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
    Task<IReadOnlyList<OpportunityEntity>> GetByIdsAsync(IReadOnlyCollection<int> ids);
    Task<int?> GetDealIdByIdAsync(int opportunityId);
    Task<bool> TryFillAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<OpportunityEntity>> GetOpenByVenueTenantIdAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
}
