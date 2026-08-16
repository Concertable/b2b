using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
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
}
