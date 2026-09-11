using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.Application.Domain.Lifecycle;
using Concertable.B2B.Opportunity.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationDashboardService : IApplicationDashboardService
{
    private readonly IApplicationRepository repository;
    private readonly IOpportunityModule opportunityModule;

    public ApplicationDashboardService(
        IApplicationRepository repository,
        IOpportunityModule opportunityModule)
    {
        this.repository = repository;
        this.opportunityModule = opportunityModule;
    }

    public async Task<int> GetVenuePendingCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var applications = await repository.GetVenueDashboardProjectionsAsync(venueTenantId, ct);
        var upcomingOpportunityIds = await GetUpcomingOpportunityIdsAsync(applications, ct);
        return applications.Count(application => upcomingOpportunityIds.Contains(application.OpportunityId));
    }

    public async Task<int> GetArtistPendingCountAsync(
        Guid artistTenantId,
        CancellationToken ct = default)
    {
        var applications = await repository.GetArtistDashboardProjectionsAsync(artistTenantId, ct);
        var upcomingOpportunityIds = await GetUpcomingOpportunityIdsAsync(applications, ct);
        return applications.Count(application =>
            application.State == ApplicationState.Applied &&
            upcomingOpportunityIds.Contains(application.OpportunityId));
    }

    public Task<IReadOnlyDictionary<int, int>> GetCountsByOpportunityIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        repository.GetCountsByOpportunityIdsAsync(opportunityIds, ct);

    public Task<IReadOnlySet<int>> GetOpportunityIdsForArtistTenantAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        repository.GetOpportunityIdsForArtistTenantAsync(artistTenantId, ct);

    private Task<IReadOnlySet<int>> GetUpcomingOpportunityIdsAsync(
        IEnumerable<ApplicationDashboardProjection> applications,
        CancellationToken ct) =>
        opportunityModule.GetUpcomingIdsAsync(
            applications.Select(application => application.OpportunityId).Distinct().ToArray(),
            ct);
}
