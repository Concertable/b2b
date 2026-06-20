using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Concertable.B2B.Tenant.Api.Authorization;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[HasPermission(Permissions.OperationsView, TenantType.Artist)]
[Route("api/[controller]")]
internal sealed class ArtistDashboardController : ControllerBase
{
    private readonly IArtistDashboardService dashboardService;

    public ArtistDashboardController(IArtistDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<ArtistDashboardKpis>> GetKpis(CancellationToken ct)
    {
        var kpis = await dashboardService.GetKpisAsync(ct);
        return kpis is null ? NoContent() : Ok(kpis);
    }
}
