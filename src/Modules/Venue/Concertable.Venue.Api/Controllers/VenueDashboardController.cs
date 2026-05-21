using Concertable.Venue.Application.DTOs;
using Concertable.Venue.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Venue.Api.Controllers;

[ApiController]
[Authorize(Policy = "VenueManager")]
[Route("api/[controller]")]
internal class VenueDashboardController : ControllerBase
{
    private readonly IVenueDashboardService dashboardService;

    public VenueDashboardController(IVenueDashboardService dashboardService)
    {
        this.dashboardService = dashboardService;
    }

    [HttpGet("kpis")]
    public async Task<ActionResult<VenueDashboardKpisDto>> GetKpis(CancellationToken ct)
    {
        var kpis = await dashboardService.GetKpisAsync(ct);
        return kpis is null ? NoContent() : Ok(kpis);
    }
}
