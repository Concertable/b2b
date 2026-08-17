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
    public async Task<ActionResult<DetailsResponse>> Get(CancellationToken ct) =>
        (await artistService.GetDetailsAsync(ct))
            .ToOkOrProblem(artist => artist.ToDetailsResponse());

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("user")]
    public async Task<ActionResult<DetailsResponse>> GetLegacy(CancellationToken ct) =>
        (await artistService.GetDetailsAsync(ct)).Match<ActionResult<DetailsResponse>>(
            artist => Ok(artist.ToDetailsResponse()),
            _ => NoContent());

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost]
    [HttpPost("/api/organization/artist")]
    public async Task<ActionResult<DetailsResponse>> Create(
        [FromForm] CreateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.CreateAsync(request, ct))
            .Map(artist => artist.ToDetailsResponse())
            .ToCreatedOrProblem(artist => $"/api/artist/{artist.Id}");

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut("{artistId:int}")]
    [HttpPut("/api/organization/artist")]
    public async Task<ActionResult<DetailsResponse>> Update(
        [FromForm] UpdateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.UpdateAsync(request, ct))
            .Map(artist => artist.ToDetailsResponse())
            .ToOkOrProblem();
}
