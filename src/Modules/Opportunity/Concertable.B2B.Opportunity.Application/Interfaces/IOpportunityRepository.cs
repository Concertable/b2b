using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Opportunity.Application.Interfaces;

internal interface IOpportunityRepository : ITenantScopedRepository<OpportunityEntity>
{
    /// <summary>
    /// Active opportunities for a venue, read <b>tracked</b> through the writing context — the
    /// management/sync path mutates these entities, so they must be change-tracked (unlike the
    /// read-only <see cref="IOpportunityReadRepository"/> projection).
    /// </summary>
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
    Task<IReadOnlyList<OpportunityEntity>> GetByIdsAsync(IReadOnlyCollection<int> ids);
    Task<int?> GetDealIdByIdAsync(int opportunityId);
}
