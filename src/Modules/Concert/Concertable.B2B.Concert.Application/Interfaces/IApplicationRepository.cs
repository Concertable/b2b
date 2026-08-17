using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationRepository : IVenueArtistTenantScopedRepository<ApplicationEntity>
{
    Task<FinancialOperation?> GetFinancialOperationAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<(LifecycleState State, PaymentVerification Verification)?> GetLifecycleAndPaymentStateAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<IEnumerable<ApplicationEntity>> GetByOpportunityIdAsync(int opportunityId);
    Task<bool> ExistsForOpportunityAndArtistTenantAsync(
        int opportunityId,
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<IEnumerable<ApplicationEntity>> GetPendingByArtistTenantIdAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<IEnumerable<ApplicationEntity>> GetRecentDeniedByArtistTenantIdAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<(ArtistReadModel, VenueReadModel)?> GetArtistAndVenueByIdAsync(int id);
    Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairByIdAsync(int applicationId);
    Task RejectAllExceptAsync(int opportunityId, int applicationId);
    Task<int?> GetDealIdByIdAsync(int applicationId);
    Task<PayeeSummary?> GetArtistPayeeAsync(int applicationId);
    Task<Guid?> GetVenueManagerIdAsync(int applicationId);
}
