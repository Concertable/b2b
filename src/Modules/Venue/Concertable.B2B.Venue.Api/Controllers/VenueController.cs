using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Application.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class VenueController : ControllerBase
{
    private readonly IVenueService venueService;

    public VenueController(IVenueService venueService)
    {
        this.venueService = venueService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VenueDetailsResponse>> GetDetailsById(int id)
    {
        return Ok((await venueService.GetDetailsByIdAsync(id)).ToDetailsResponse());
    }

    [Authorize(Policy = "VenueManager")]
    [HttpGet("user")]
    public async Task<ActionResult<VenueDetailsResponse>> GetDetailsForCurrentUser()
    {
        var venue = await venueService.GetDetailsForCurrentUserAsync();
        return venue is null ? NoContent() : Ok(venue.ToDetailsResponse());
    }

    [Authorize(Policy = "VenueManager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateVenueRequest request)
    {
        var venueDto = await venueService.CreateAsync(request);
        return CreatedAtAction(nameof(GetDetailsById), new { Id = venueDto.Id }, venueDto);
    }

    [Authorize(Policy = "VenueManager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateVenueRequest request)
    {
        return Ok(await venueService.UpdateAsync(id, request));
    }

    [Authorize(Policy = "Admin")]
    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await venueService.ApproveAsync(id);
        return NoContent();
    }

    [HttpGet("{id}/ownership")]
    public async Task<ActionResult<bool>> IsOwner(int id)
    {
        return Ok(await venueService.OwnsVenueAsync(id));
    }
}
