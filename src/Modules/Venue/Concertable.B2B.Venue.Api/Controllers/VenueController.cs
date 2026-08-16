using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.User.Api.Authorization;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route("api/venue")]
internal sealed class VenueController : ControllerBase
{
    private readonly IVenueService venueService;

    public VenueController(IVenueService venueService)
    {
        this.venueService = venueService;
    }

    [HttpGet("{venueId:int}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(
        int venueId,
        CancellationToken ct)
    {
        return (await venueService.GetDetailsByIdAsync(venueId, ct))
            .ToOkOrProblem(venue => venue.ToDetailsResponse());
    }

    [Admin]
    [HttpPatch("{venueId:int}/approve")]
    public async Task<IActionResult> Approve(int venueId, CancellationToken ct)
    {
        return (await venueService.ApproveAsync(venueId, ct)).ToNoContentOrProblem();
    }

    [HttpGet("{venueId:int}/ownership")]
    public async Task<ActionResult<bool>> IsOwner(int venueId, CancellationToken ct)
    {
        return Ok(await venueService.OwnsVenueAsync(venueId, ct));
    }
}
