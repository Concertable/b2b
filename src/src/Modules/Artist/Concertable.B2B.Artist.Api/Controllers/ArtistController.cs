using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route($"api/{RouteSegment}")]
internal sealed class ArtistController : ControllerBase
{
    internal const string RouteSegment = "artist";

    private readonly IArtistService artistService;

    public ArtistController(IArtistService artistService)
    {
        this.artistService = artistService;
    }

    [EnableRateLimiting(RateLimitPolicies.PublicRead)]
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
    [HttpGet($"/api/organization/{RouteSegment}")]
    public async Task<ActionResult<DetailsResponse>> GetDetails(CancellationToken ct) =>
        (await artistService.GetDetailsAsync(ct))
            .ToOkOrNoContent(artist => artist.ToDetailsResponse());

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [EnableRateLimiting(RateLimitPolicies.ProfileImage)]
    [HttpPost($"/api/organization/{RouteSegment}")]
    public async Task<ActionResult<DetailsResponse>> Create(
        [FromForm] CreateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.CreateAsync(request, ct))
            .ToCreatedOrProblem(
                artist => artist.ToDetailsResponse(),
                artist => $"/api/artist/{artist.Id}");

    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.ProfileEdit)]
    [EnableRateLimiting(RateLimitPolicies.ProfileImage)]
    [HttpPut($"/api/organization/{RouteSegment}")]
    public async Task<ActionResult<DetailsResponse>> Update(
        [FromForm] UpdateArtistRequest request,
        CancellationToken ct) =>
        (await artistService.UpdateAsync(request, ct))
            .ToOkOrProblem(artist => artist.ToDetailsResponse());
}
