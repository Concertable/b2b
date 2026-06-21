using Concertable.B2B.Artist.Api.Mappers;
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
        return Ok((await artistService.GetDetailsByIdAsync(id)).ToDetailsResponse());
    }

    [HasPermission(Permissions.OperationsView)]
    [HttpGet("user")]
    public async Task<ActionResult<ArtistDetailsResponse>> GetDetailsForCurrentUser()
    {
        var artist = await artistService.GetDetailsForCurrentUserAsync();
        return artist is null ? NoContent() : Ok(artist.ToDetailsResponse());
    }

    [HasPermission(Permissions.ProfileEdit)]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateArtistRequest request)
    {
        var artistDto = await artistService.CreateAsync(request);
        return CreatedAtAction(nameof(GetDetailsById), new { Id = artistDto.Id }, artistDto);
    }

    [HasPermission(Permissions.ProfileEdit)]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateArtistRequest request)
    {
        return Ok(await artistService.UpdateAsync(id, request));
    }
}
