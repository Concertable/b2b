using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationRepository : IVenueArtistTenantScopedRepository<ApplicationEntity>
{
    Task<ApplicationEntity?> GetWithVerifyPaymentByIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationEntity>> GetByOpportunityIdAsync(
        int opportunityId,
        CancellationToken ct = default);
    Task<bool> ExistsForOpportunityAndArtistTenantAsync(
        int opportunityId,
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationEntity>> GetByArtistTenantIdAndStateAsync(
        Guid artistTenantId,
        ApplicationState state,
        CancellationToken ct = default);
    Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairByIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default);
}
