namespace Concertable.Concert.Application.Interfaces;

internal interface IApplicationRepository : IIdRepository<ApplicationEntity>
{
    Task<IEnumerable<ApplicationEntity>> GetByOpportunityIdAsync(int opportunityId);
    Task<IEnumerable<ApplicationEntity>> GetPendingByArtistIdAsync(int id);
    Task<IEnumerable<ApplicationEntity>> GetRecentDeniedByArtistIdAsync(int id);
    Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id);
    Task RejectAllExceptAsync(int opportunityId, int applicationId);
    Task<int?> GetContractIdByIdAsync(int applicationId);
}
