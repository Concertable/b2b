using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Opportunity.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertDashboardService : IConcertDashboardService
{
    private readonly IConcertDashboardRepository repository;
    private readonly IApplicationModule applications;
    private readonly IOpportunityModule opportunities;

    public ConcertDashboardService(
        IConcertDashboardRepository repository,
        IApplicationModule applications,
        IOpportunityModule opportunities)
    {
        this.repository = repository;
        this.applications = applications;
        this.opportunities = opportunities;
    }

    public async Task<Option<VenueDashboardCounts>> GetVenueCountsAsync(
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var concertCounts = await repository.GetVenueCountsAsync(venueTenantId, ct);
        if (concertCounts is null)
            return Option.None<VenueDashboardCounts>();

        var applicationCount = await applications.GetVenuePendingCountAsync(venueTenantId, ct);
        var opportunityCount = await opportunities.GetOpenCountAsync(venueTenantId, ct);
        return Option.Some(new VenueDashboardCounts(
            applicationCount,
            opportunityCount,
            concertCounts.UpcomingConcerts,
            concertCounts.AwaitingDoorRevenue));
    }

    public async Task<Option<ArtistDashboardCounts>> GetArtistCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default)
    {
        var concertCounts = await repository.GetArtistCountsAsync(artistTenantId, ct);
        if (concertCounts is null)
            return Option.None<ArtistDashboardCounts>();

        var applicationCounts = await applications.GetArtistDashboardCountsAsync(artistTenantId, ct);
        return Option.Some(new ArtistDashboardCounts(
            applicationCounts.PendingApplications,
            applicationCounts.AcceptedAwaitingCheckout,
            concertCounts.UpcomingConcerts));
    }

    public Task<IReadOnlyList<ManagerSettlementContext>> GetManagerSettlementContextsAsync(
        IReadOnlyCollection<int> bookingIds,
        CancellationToken ct = default) =>
        repository.GetManagerSettlementContextsAsync(bookingIds, ct);
}
