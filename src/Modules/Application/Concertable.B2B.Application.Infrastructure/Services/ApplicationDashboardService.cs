using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Models;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.State;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Opportunity.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationDashboardService : IApplicationDashboardService
{
    private readonly IApplicationRepository repository;
    private readonly IOpportunityModule opportunities;

    public ApplicationDashboardService(
        IApplicationRepository repository,
        IOpportunityModule opportunities)
    {
        this.repository = repository;
        this.opportunities = opportunities;
    }

    public async Task<int> GetVenuePendingCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var applications = await repository.GetVenueDashboardProjectionsAsync(venueTenantId, ct);
        var upcomingOpportunityIds = await GetUpcomingOpportunityIdsAsync(applications, ct);
        return applications.Count(application => upcomingOpportunityIds.Contains(application.OpportunityId));
    }

    public async Task<ArtistApplicationDashboardCounts> GetArtistCountsAsync(
        Guid artistTenantId,
        IReadOnlySet<DealType> acceptCheckoutDealTypes,
        CancellationToken ct = default)
    {
        var applications = await repository.GetArtistDashboardProjectionsAsync(artistTenantId, ct);
        var upcomingOpportunityIds = await GetUpcomingOpportunityIdsAsync(applications, ct);
        return new ArtistApplicationDashboardCounts(
            applications.Count(application =>
                application.State == ApplicationState.Applied &&
                upcomingOpportunityIds.Contains(application.OpportunityId)),
            applications.Count(application =>
                application.State == ApplicationState.Accepted &&
                acceptCheckoutDealTypes.Contains(application.DealType) &&
                upcomingOpportunityIds.Contains(application.OpportunityId)));
    }

    private Task<IReadOnlySet<int>> GetUpcomingOpportunityIdsAsync(
        IEnumerable<ApplicationDashboardProjection> applications,
        CancellationToken ct) =>
        opportunities.GetUpcomingIdsAsync(
            applications.Select(application => application.OpportunityId).Distinct().ToArray(),
            ct);
}
