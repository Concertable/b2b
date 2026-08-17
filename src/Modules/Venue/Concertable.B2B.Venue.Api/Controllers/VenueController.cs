using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
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

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("/api/organization/venue")]
    public async Task<ActionResult<DetailsResponse>> Get(CancellationToken ct) =>
        (await venueService.GetDetailsAsync(ct))
            .ToOkOrProblem(venue => venue.ToDetailsResponse());

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost("/api/organization/venue")]
    public async Task<ActionResult<DetailsResponse>> Create(
        [FromForm] CreateVenueRequest request,
        CancellationToken ct) =>
        (await venueService.CreateAsync(request, ct))
            .Map(venue => venue.ToDetailsResponse())
            .ToCreatedOrProblem(venue => $"/api/venue/{venue.Id}");

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut("/api/organization/venue")]
    public async Task<ActionResult<DetailsResponse>> Update(
        [FromForm] UpdateVenueRequest request,
        CancellationToken ct) =>
        (await venueService.UpdateAsync(request, ct))
            .Map(venue => venue.ToDetailsResponse())
            .ToOkOrProblem();
}
