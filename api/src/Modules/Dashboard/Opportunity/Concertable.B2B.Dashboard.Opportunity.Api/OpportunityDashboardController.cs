using Concertable.B2B.Dashboard.Opportunity.Application;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Dashboard.Opportunity.Api;

[ApiController]
[Route("api/opportunity")]
internal sealed class OpportunityDashboardController : ControllerBase
{
    private readonly IOpportunityDashboardService service;

    public OpportunityDashboardController(IOpportunityDashboardService service) =>
        this.service = service;

    [HttpGet("venue/current")]
    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<OpportunityMetricsResponse>>>
        GetOpen(CancellationToken ct) =>
        (await service.GetOpenAsync(ct))
            .ToOkOrProblem(metrics => metrics.ToResponses());

    [HttpGet("artist/recommended")]
    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<OpportunityMatchResponse>>>
        GetRecommended(CancellationToken ct) =>
        (await service.GetRecommendedAsync(ct))
            .ToOkOrProblem(matches => matches.ToResponses());
}
