using Concertable.B2B.Dashboard.Artist.Application;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Dashboard.Artist.Api;

[ApiController]
[RequiredTenantType(TenantType.Artist)]
[HasPermission(SharedPermissions.OperationsView)]
[Route("api/artist-dashboard")]
internal sealed class ArtistDashboardController : ControllerBase
{
    private readonly IArtistDashboardService service;

    public ArtistDashboardController(IArtistDashboardService service) =>
        this.service = service;

    [HttpGet("kpis")]
    public async Task<ActionResult<ArtistDashboardKpis>> GetKpis(CancellationToken ct) =>
        (await service.GetAsync(ct)).ToOkOrNoContent();

    [HttpGet("overview")]
    public async Task<ActionResult<ArtistDashboardOverview>> GetOverview(CancellationToken ct) =>
        (await service.GetOverviewAsync(ct)).ToOkOrNoContent();

    [HttpGet("charts/payouts")]
    public async Task<ActionResult<IReadOnlyList<MonthlyRevenuePoint>>> GetPayouts(CancellationToken ct) =>
        Ok(await service.GetPayoutsAsync(ct));

    [HttpGet("activity")]
    public async Task<ActionResult<IReadOnlyList<ActivityItemDto>>> GetActivity(CancellationToken ct) =>
        Ok(await service.GetActivityAsync(ct));
}
