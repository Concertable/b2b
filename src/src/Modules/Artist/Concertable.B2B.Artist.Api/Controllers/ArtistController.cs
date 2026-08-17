using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            .ToOkOrNotFound(artist => artist.ToDetailsResponse());
    }

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("/api/organization/[controller]")]
    public async Task<ActionResult<DetailsResponse>> GetDetails(CancellationToken ct) =>
        (await artistService.GetDetailsAsync(ct))
            .ToOkOrNoContent(artist => artist.ToDetailsResponse());

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost("/api/organization/[controller]")]
    public async Task<ActionResult<DetailsResponse>> Create(
        [FromForm] CreateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.CreateAsync(request, ct))
            .ToCreatedOrProblem(
                artist => artist.ToDetailsResponse(),
                artist => $"/api/artist/{artist.Id}");

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut("/api/organization/[controller]")]
    public async Task<ActionResult<DetailsResponse>> Update(
        [FromForm] UpdateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.UpdateAsync(request, ct))
            .ToOkOrProblem(artist => artist.ToDetailsResponse());
}
