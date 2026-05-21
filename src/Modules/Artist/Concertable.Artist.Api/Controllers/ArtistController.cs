using Concertable.Artist.Api.Mappers;
using Concertable.Artist.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Artist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class ArtistController : ControllerBase
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

    [Authorize(Policy = "ArtistManager")]
    [HttpGet("user")]
    public async Task<ActionResult<ArtistDetailsResponse>> GetDetailsForCurrentUser()
    {
        var artist = await artistService.GetDetailsForCurrentUserAsync();
        return artist is null ? NoContent() : Ok(artist.ToDetailsResponse());
    }

    [Authorize(Policy = "ArtistManager")]
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] CreateArtistRequest request)
    {
        var artistDto = await artistService.CreateAsync(request);
        return CreatedAtAction(nameof(GetDetailsById), new { Id = artistDto.Id }, artistDto);
    }

    [Authorize(Policy = "ArtistManager")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateArtistRequest request)
    {
        return Ok(await artistService.UpdateAsync(id, request));
    }
}
