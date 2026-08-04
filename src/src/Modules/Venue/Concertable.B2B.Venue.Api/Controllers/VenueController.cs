using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Api.Authorization;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequiredTenantType(TenantType.Venue)]
internal sealed class VenueController : ControllerBase
{
    private readonly IVenueService venueService;

    public VenueController(IVenueService venueService)
    {
        this.venueService = venueService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(int id)
    {
        return (await venueService.GetDetailsByIdAsync(id))
            .Map(venue => venue.ToDetailsResponse())
            .ToOkActionResult();
    }

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("user")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsForCurrentUser() =>
        (await venueService.GetDetailsForCurrentUserAsync())
            .Map(venue => venue.ToDetailsResponse())
            .ToOkActionResult();

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost]
    public async Task<ActionResult<VenueDetails>> Create([FromForm] CreateVenueRequest request)
    {
        return (await venueService.CreateAsync(request)).ToActionResult(
            venue => CreatedAtAction(nameof(GetDetailsById), new { Id = venue.Id }, venue));
    }

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut("{id}")]
    public async Task<ActionResult<VenueDetails>> Update(int id, [FromForm] UpdateVenueRequest request)
    {
        return (await venueService.UpdateAsync(id, request)).ToOkActionResult();
    }

    [Admin]
    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        return (await venueService.ApproveAsync(id)).ToNoContentActionResult();
    }

    [HttpGet("{id}/ownership")]
    public async Task<ActionResult<bool>> IsOwner(int id)
    {
        return Ok(await venueService.OwnsVenueAsync(id));
    }
}
