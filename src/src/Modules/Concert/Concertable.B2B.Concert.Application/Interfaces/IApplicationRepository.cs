using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationRepository : IVenueArtistTenantScopedRepository<ApplicationEntity>
{
    Task<(LifecycleState State, PaymentVerification Verification)?> GetLifecycleAndPaymentStateAsync(int applicationId);
    Task<IEnumerable<ApplicationEntity>> GetByOpportunityIdAsync(int opportunityId);
    Task<bool> ExistsForOpportunityAndArtistAsync(int opportunityId, int artistId);
    Task<IEnumerable<ApplicationEntity>> GetPendingByArtistIdAsync(int id);
    Task<IEnumerable<ApplicationEntity>> GetRecentDeniedByArtistIdAsync(int id);
    Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id);
    Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairByIdAsync(int applicationId);
    Task RejectAllExceptAsync(int opportunityId, int applicationId);
    Task<int?> GetDealIdByIdAsync(int applicationId);
    Task<PayeeSummary?> GetArtistPayeeAsync(int applicationId);
    Task<Guid?> GetVenueManagerIdAsync(int applicationId);
}
