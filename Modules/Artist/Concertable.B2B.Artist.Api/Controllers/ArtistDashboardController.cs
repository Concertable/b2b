using Concertable.B2B.Artist.Application.DTOs;
using Concertable.B2B.Artist.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Authorize(Policy = "ArtistManager")]
[Route("api/[controller]")]
internal class ArtistDashboardController : ControllerBase
{
    private readonly IArtistDashboardService dashboardService;

    public ArtistDashboardController(IArtistDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<ArtistDashboardKpisDto>> GetKpis(CancellationToken ct)
    {
        var kpis = await dashboardService.GetKpisAsync(ct);
        return kpis is null ? NoContent() : Ok(kpis);
    }
}
