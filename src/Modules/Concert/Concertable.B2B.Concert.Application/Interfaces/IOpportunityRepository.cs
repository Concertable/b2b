using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IOpportunityRepository : ITenantScopedRepository<OpportunityEntity>
{
    Task<OpportunityEntity?> GetWithVenueByIdAsync(int id);
    Task<OpportunityEntity?> GetByApplicationIdAsync(int id);
    Task<Guid?> GetOwnerByIdAsync(int id);
    Task<int?> GetContractIdByIdAsync(int opportunityId);
    Task<(string Name, Guid UserId)?> GetVenueSummaryByIdAsync(int opportunityId);
}
