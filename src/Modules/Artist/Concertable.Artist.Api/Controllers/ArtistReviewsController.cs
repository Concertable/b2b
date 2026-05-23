using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Artist.Api.Controllers;

[ApiController]
[Route("api/artists/{artistId}/reviews")]
internal class ArtistReviewsController : ControllerBase
{
    private readonly IArtistReviewService reviewService;

    public ArtistReviewsController(IArtistReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReviewSummaryDto>> GetSummary(int artistId) =>
        Ok(await reviewService.GetSummaryAsync(artistId));
}
