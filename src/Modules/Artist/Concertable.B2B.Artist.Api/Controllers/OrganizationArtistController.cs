using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route("api/organization/artist")]
[RequiredTenantType(TenantType.Artist)]
internal sealed class OrganizationArtistController : ControllerBase
{
    private readonly IArtistService artistService;

    public OrganizationArtistController(IArtistService artistService)
    {
        this.artistService = artistService;
    }

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet]
    public async Task<ActionResult<DetailsResponse>> Get(CancellationToken ct) =>
        (await artistService.GetDetailsForActiveTenantAsync(ct))
            .ToOkOrProblem(artist => artist.ToDetailsResponse());

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost]
    public async Task<ActionResult<DetailsResponse>> Create(
        [FromForm] CreateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.CreateForActiveTenantAsync(request, ct))
            .Map(artist => artist.ToDetailsResponse())
            .ToCreatedOrProblem(artist => $"/api/artist/{artist.Id}");

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut]
    public async Task<ActionResult<DetailsResponse>> Update(
        [FromForm] UpdateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.UpdateForActiveTenantAsync(request, ct))
            .Map(artist => artist.ToDetailsResponse())
            .ToOkOrProblem();
}
