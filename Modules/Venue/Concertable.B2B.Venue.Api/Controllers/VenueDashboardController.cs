using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[HasPermission(Permissions.OperationsView, TenantType.Venue)]
[Route("api/[controller]")]
internal sealed class VenueDashboardController : ControllerBase
{
    private readonly IVenueDashboardService dashboardService;

    public VenueDashboardController(IVenueDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<VenueDashboardKpis>> GetKpis(CancellationToken ct)
    {
        var kpis = await dashboardService.GetKpisAsync(ct);
        return kpis is null ? NoContent() : Ok(kpis);
    }
}
