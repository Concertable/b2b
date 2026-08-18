using Concertable.B2B.Opportunity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure;

internal sealed class OpportunityModule : IOpportunityModule
{
    private readonly IOpportunityReadDbContext readContext;
    private readonly IOpportunityHandoffRepository handoffRepository;
    private readonly IOpportunityDashboardService dashboardService;

    public OpportunityModule(
        IOpportunityReadDbContext readContext,
        IOpportunityHandoffRepository handoffRepository,
        IOpportunityDashboardService dashboardService)
    {
        this.readContext = readContext;
        this.handoffRepository = handoffRepository;
        this.dashboardService = dashboardService;
    }

    public async Task<Option<OpportunityDetails>> GetDetailsAsync(
        int opportunityId,
        CancellationToken ct = default)
    {
        var details = await readContext.Opportunities
            .Where(opportunity => opportunity.Id == opportunityId)
            .Select(opportunity => new OpportunityDetails(
                opportunity.Id,
                opportunity.VenueId,
                opportunity.TenantId,
                opportunity.DealId,
                opportunity.Period.Start,
                opportunity.Period.End,
                opportunity.Genres))
            .FirstOrDefaultAsync(ct);

        return details is null ? Option.None<OpportunityDetails>() : Option.Some(details);
    }

    public Task<bool> TryClaimAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default) =>
        handoffRepository.TryClaimAsync(opportunityId, venueTenantId, ct);

    public Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        dashboardService.GetUpcomingIdsAsync(opportunityIds, ct);

    public Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetOpenCountAsync(venueTenantId, ct);
}
