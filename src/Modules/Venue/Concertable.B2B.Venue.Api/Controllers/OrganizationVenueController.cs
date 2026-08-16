using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route("api/organization/venue")]
[RequiredTenantType(TenantType.Venue)]
internal sealed class OrganizationVenueController : ControllerBase
{
    private readonly IVenueService venueService;

    public OrganizationVenueController(IVenueService venueService)
    {
        this.venueService = venueService;
    }

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet]
    public async Task<ActionResult<DetailsResponse>> Get(CancellationToken ct) =>
        (await venueService.GetDetailsForActiveTenantAsync(ct))
            .ToOkOrProblem(venue => venue.ToDetailsResponse());

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost]
    public async Task<ActionResult<DetailsResponse>> Create(
        [FromForm] CreateVenueRequest request,
        CancellationToken ct) =>
        (await venueService.CreateForActiveTenantAsync(request, ct))
            .Map(venue => venue.ToDetailsResponse())
            .ToCreatedOrProblem(venue => $"/api/venue/{venue.Id}");

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut]
    public async Task<ActionResult<DetailsResponse>> Update(
        [FromForm] UpdateVenueRequest request,
        CancellationToken ct) =>
        (await venueService.UpdateForActiveTenantAsync(request, ct))
            .Map(venue => venue.ToDetailsResponse())
            .ToOkOrProblem();
}
