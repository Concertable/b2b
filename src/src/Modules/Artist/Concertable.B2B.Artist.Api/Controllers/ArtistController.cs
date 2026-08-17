using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route("api/artist")]
internal sealed class ArtistController : ControllerBase
{
    private readonly IArtistService artistService;

    public ArtistController(IArtistService artistService)
    {
        this.artistService = artistService;
    }

    [HttpGet("{artistId:int}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(
        int artistId,
        CancellationToken ct)
    {
        return (await artistService.GetDetailsByIdAsync(artistId, ct))
            .ToOkOrProblem(artist => artist.ToDetailsResponse());
    }

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("/api/organization/artist")]
    public async Task<ActionResult<DetailsResponse>> GetForActiveTenant(CancellationToken ct) =>
        (await artistService.GetDetailsForActiveTenantAsync(ct))
            .ToOkOrProblem(artist => artist.ToDetailsResponse());

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost("/api/organization/artist")]
    public async Task<ActionResult<DetailsResponse>> CreateForActiveTenant(
        [FromForm] CreateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.CreateForActiveTenantAsync(request, ct))
            .Map(artist => artist.ToDetailsResponse())
            .ToCreatedOrProblem(artist => $"/api/artist/{artist.Id}");

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut("/api/organization/artist")]
    public async Task<ActionResult<DetailsResponse>> UpdateForActiveTenant(
        [FromForm] UpdateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.UpdateForActiveTenantAsync(request, ct))
            .Map(artist => artist.ToDetailsResponse())
            .ToOkOrProblem();
}
