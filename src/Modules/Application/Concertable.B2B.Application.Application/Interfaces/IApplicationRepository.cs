using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationRepository : IVenueArtistTenantScopedRepository<ApplicationEntity>
{
    /// <summary>
    /// Saves pending changes. A lost race on the aggregate's <c>State</c> concurrency token returns
    /// <see langword="false"/>; every other failure propagates.
    /// </summary>
    Task<bool> TrySaveChangesAsync(CancellationToken ct = default);

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
    Task<IReadOnlyList<ApplicationEntity>> GetByVenueTenantIdAndStateAsync(
        Guid venueTenantId,
        ApplicationState state,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationEntity>> GetCurrentByArtistTenantIdAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<(Guid VenueTenantId, Guid ArtistTenantId)?> GetTenantPairByIdAsync(
        int applicationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<int>> RejectAllExceptAsync(
        int opportunityId,
        int applicationId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationDashboardProjection>> GetVenueDashboardProjectionsAsync(
        Guid venueTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyList<ApplicationDashboardProjection>> GetArtistDashboardProjectionsAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, int>> GetCountsByOpportunityIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default);
    Task<IReadOnlySet<int>> GetOpportunityIdsForArtistTenantAsync(
        Guid artistTenantId,
        CancellationToken ct = default);
}
