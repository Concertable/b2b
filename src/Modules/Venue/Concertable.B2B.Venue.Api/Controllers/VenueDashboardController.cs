using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[RequiredTenantType(TenantType.Venue)]
[HasPermission(SharedPermissions.OperationsView)]
[Route("api/[controller]")]
internal sealed class VenueDashboardController : ControllerBase
{
    private readonly IVenueDashboardService dashboardService;

    public VenueDashboardController(IVenueDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<VenueDashboardOverview>> GetOverview(CancellationToken ct)
    {
        var overview = await dashboardService.GetOverviewAsync(ct);
        return overview is null ? NoContent() : Ok(overview);
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<VenueDashboardKpis>> GetKpis(CancellationToken ct)
    {
        var kpis = await dashboardService.GetKpisAsync(ct);
        return kpis is null ? NoContent() : Ok(kpis);
    }

    [HttpGet("charts/ticket-revenue")]
    public async Task<ActionResult<IReadOnlyList<MonthlyRevenuePoint>>> GetTicketRevenue(CancellationToken ct) =>
        Ok(await dashboardService.GetTicketRevenueAsync(ct));

    [HttpGet("settlements")]
    public async Task<ActionResult<IReadOnlyList<Settlement>>> GetSettlements(CancellationToken ct) =>
        Ok(await dashboardService.GetSettlementsAsync(ct));

    [HttpGet("activity")]
    public async Task<ActionResult<IReadOnlyList<ActivityItemDto>>> GetActivity(
        [FromQuery] int take,
        CancellationToken ct) =>
        Ok(await dashboardService.GetActivityAsync(Math.Clamp(take, 1, 20), ct));
}
