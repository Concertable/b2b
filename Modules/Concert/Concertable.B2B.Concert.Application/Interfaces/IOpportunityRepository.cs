using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IOpportunityRepository : ITenantScopedRepository<OpportunityEntity>
{
    /// <summary>
    /// Active opportunities for a venue, read <b>tracked</b> through the writing context — the
    /// management/sync path mutates these entities, so they must be change-tracked (unlike the
    /// read-only <see cref="IPublicOpportunityRepository"/> projection).
    /// </summary>
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
    Task<OpportunityEntity?> GetWithVenueByIdAsync(int id);
    Task<OpportunityEntity?> GetByApplicationIdAsync(int id);
    Task<Guid?> GetOwnerByIdAsync(int id);
    Task<int?> GetContractIdByIdAsync(int opportunityId);
    Task<(string Name, Guid UserId)?> GetVenueSummaryByIdAsync(int opportunityId);
}
