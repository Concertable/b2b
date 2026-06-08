using Concertable.B2B.Concert.Domain.Entities;
using Concertable.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IOpportunityRepository : IRepository<OpportunityEntity>
{
    Task<IPagination<OpportunityEntity>> GetActiveByVenueIdAsync(int id, IPageParams pageParams);
    Task<IEnumerable<OpportunityEntity>> GetActiveByVenueIdAsync(int venueId);
    Task<OpportunityEntity?> GetWithVenueByIdAsync(int id);
    Task<OpportunityEntity?> GetByApplicationIdAsync(int id);
    Task<Guid?> GetOwnerByIdAsync(int id);
    Task<int?> GetContractIdByIdAsync(int opportunityId);
}
