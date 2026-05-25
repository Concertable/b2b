using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route("api/artists/{artistId}/reviews")]
internal class ArtistReviewsController : ControllerBase
{
    private readonly IArtistReviewService reviewService;

    public ArtistReviewsController(IArtistReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<IPagination<ReviewDto>>> GetReviews(int artistId, [FromQuery] PageParams pageParams) =>
        Ok(await reviewService.GetPagedAsync(artistId, pageParams));

    [HttpGet("summary")]
    public async Task<ActionResult<ReviewSummaryDto>> GetSummary(int artistId) =>
        Ok(await reviewService.GetSummaryAsync(artistId));
}
