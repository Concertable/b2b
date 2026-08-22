using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[RequiredTenantType(TenantType.Artist)]
[HasPermission(SharedPermissions.OperationsView)]
[Route("api/[controller]")]
internal sealed class ArtistDashboardController : ControllerBase
{
    private readonly IArtistDashboardService dashboardService;

    public ArtistDashboardController(IArtistDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ArtistDashboardOverview>> GetOverview(CancellationToken ct)
        => (await dashboardService.GetOverviewAsync(ct)).ToOkOrNoContent();

    [HttpGet("kpis")]
    public async Task<ActionResult<ArtistDashboardKpis>> GetKpis(CancellationToken ct)
    {
        return (await dashboardService.GetKpisAsync(ct)).ToOkOrNoContent();
    }

    [HttpGet("charts/payouts")]
    public async Task<ActionResult<IReadOnlyList<MonthlyRevenuePoint>>> GetPayouts(CancellationToken ct) =>
        Ok(await dashboardService.GetPayoutsAsync(ct));

    [HttpGet("activity")]
    public async Task<ActionResult<IReadOnlyList<ActivityItemDto>>> GetActivity(CancellationToken ct) =>
        Ok(await dashboardService.GetActivityAsync(ct));
}
