using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Errors;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[TenantPersona(TenantType.Artist)]
internal sealed class ArtistController : ControllerBase
{
    private readonly IArtistService artistService;

    public ArtistController(IArtistService artistService)
    {
        this.artistService = artistService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ArtistDetailsResponse>> GetDetailsById(int id)
    {
        return (await artistService.GetDetailsByIdAsync(id))
            .Map(artist => artist.ToDetailsResponse())
            .OrFailure(() => GetArtistError.NotFound(id))
            .ToOkActionResult();
    }

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("user")]
    public async Task<ActionResult<ArtistDetailsResponse>> GetDetailsForCurrentUser()
    {
        var artist = await artistService.GetDetailsForCurrentUserAsync();
        return artist.Match<ActionResult<ArtistDetailsResponse>>(
            value => Ok(value.ToDetailsResponse()),
            () => NoContent());
    }

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost]
    public async Task<ActionResult<ArtistDetails>> Create([FromForm] CreateArtistRequest request)
    {
        return (await artistService.CreateAsync(request)).ToActionResult(
            artist => CreatedAtAction(nameof(GetDetailsById), new { Id = artist.Id }, artist));
    }

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ArtistDetails>> Update(int id, [FromForm] UpdateArtistRequest request)
    {
        return (await artistService.UpdateAsync(id, request)).ToOkActionResult();
    }
}
