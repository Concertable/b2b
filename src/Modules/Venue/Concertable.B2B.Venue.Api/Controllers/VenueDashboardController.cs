using Concertable.B2B.User.Api.Authorization;
using Concertable.B2B.Venue.Application.DTOs;
using Concertable.B2B.Venue.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[VenueManager]
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
