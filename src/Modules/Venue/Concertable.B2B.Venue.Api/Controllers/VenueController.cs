using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Application.Interfaces;
using Concertable.B2B.Venue.Application.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route($"api/{RouteSegment}")]
internal sealed class VenueController : ControllerBase
{
    internal const string RouteSegment = "venue";

    private readonly IVenueService venueService;

    public VenueController(IVenueService venueService)
    {
        this.venueService = venueService;
    }

    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
    [HttpGet("{venueId:int}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(
        int venueId,
        CancellationToken ct)
    {
        return (await venueService.GetDetailsByIdAsync(venueId, ct))
            .ToOkOrNotFound(venue => venue.ToDetailsResponse());
    }

    [HttpGet("{venueId:int}/ownership")]
    public async Task<ActionResult<bool>> IsOwner(int venueId, CancellationToken ct)
    {
        return Ok(await venueService.OwnsVenueAsync(venueId, ct));
    }

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet($"/api/organization/{RouteSegment}")]
    public async Task<ActionResult<DetailsResponse>> GetDetails(CancellationToken ct) =>
        (await venueService.GetDetailsAsync(ct))
            .ToOkOrNoContent(venue => venue.ToDetailsResponse());

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [EnableRateLimiting(RateLimitPolicies.ProfileImage)]
    [HttpPost($"/api/organization/{RouteSegment}")]
    public async Task<ActionResult<DetailsResponse>> Create(
        [FromForm] CreateVenueRequest request,
        CancellationToken ct) =>
        (await venueService.CreateAsync(request, ct))
            .ToCreatedOrProblem(
                venue => venue.ToDetailsResponse(),
                venue => $"/api/venue/{venue.Id}");

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [EnableRateLimiting(RateLimitPolicies.ProfileImage)]
    [HttpPut($"/api/organization/{RouteSegment}")]
    public async Task<ActionResult<DetailsResponse>> Update(
        [FromForm] UpdateVenueRequest request,
        CancellationToken ct) =>
        (await venueService.UpdateAsync(request, ct))
            .ToOkOrProblem(venue => venue.ToDetailsResponse());
}
