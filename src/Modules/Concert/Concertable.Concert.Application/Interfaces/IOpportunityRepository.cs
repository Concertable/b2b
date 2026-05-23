using Concertable.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.Concert.Application.Interfaces;

internal interface IOpportunityRepository : IIdRepository<OpportunityEntity>
{
    new Task<OpportunityEntity?> GetByIdAsync(int id);
    Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int id, IPageParams pageParams);
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
    Task<OpportunityEntity?> GetWithVenueByIdAsync(int id);
    Task<OpportunityEntity?> GetByApplicationIdAsync(int id);
    Task<Guid?> GetOwnerByIdAsync(int id);
    Task<int?> GetContractIdByIdAsync(int opportunityId);
}
