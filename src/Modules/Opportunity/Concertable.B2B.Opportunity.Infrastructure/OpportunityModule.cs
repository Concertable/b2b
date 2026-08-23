namespace Concertable.B2B.Opportunity.Infrastructure;

internal sealed class OpportunityModule : IOpportunityModule
{
    private readonly IOpportunityHandoffService handoffService;
    private readonly IOpportunityDashboardService dashboardService;

    public OpportunityModule(
        IOpportunityHandoffService handoffService,
        IOpportunityDashboardService dashboardService)
    {
        this.handoffService = handoffService;
        this.dashboardService = dashboardService;
    }

    public async Task<Option<OpportunityDetails>> GetDetailsAsync(
        int opportunityId,
        CancellationToken ct = default)
    {
        var details = await handoffService.GetDetailsAsync(opportunityId, ct);

        return ToOption(details);
    }

    public async Task<Option<OpportunityDetails>> GetOpenDetailsAsync(
        int opportunityId,
        CancellationToken ct = default)
    {
        var details = await handoffService.GetOpenDetailsAsync(opportunityId, ct);

        return ToOption(details);
    }

    public async Task<IReadOnlyList<OpportunityDetails>> GetDetailsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        (await handoffService.GetDetailsAsync(opportunityIds, ct))
            .Select(ToDetails)
            .ToList();

    private static OpportunityDetails ToDetails(OpportunityHandoffDto details) =>
        new(
            details.Id,
            details.VenueId,
            details.TenantId,
            details.DealId,
            details.Start,
            details.End,
            details.Genres);

    private static Option<OpportunityDetails> ToOption(OpportunityHandoffDto? details)
    {

        return details is null
            ? Option.None<OpportunityDetails>()
            : Option.Some(ToDetails(details));
    }

    public Task<bool> TryClaimAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default) =>
        handoffService.TryClaimAsync(opportunityId, venueTenantId, ct);

    public Task<IReadOnlySet<int>> GetUpcomingIdsAsync(
        IReadOnlyCollection<int> opportunityIds,
        CancellationToken ct = default) =>
        dashboardService.GetUpcomingIdsAsync(opportunityIds, ct);

    public Task<int> GetOpenCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetOpenCountAsync(venueTenantId, ct);
}
