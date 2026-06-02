using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route("api/venues/{venueId}/reviews")]
internal sealed class VenueReviewsController : ControllerBase
{
    private readonly IVenueReviewService reviewService;

    public VenueReviewsController(IVenueReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<IPagination<ReviewDto>>> GetReviews(int venueId, [FromQuery] PageParams pageParams) =>
        Ok(await reviewService.GetPagedAsync(venueId, pageParams));

    [HttpGet("summary")]
    public async Task<ActionResult<ReviewSummary>> GetSummary(int venueId) =>
        Ok(await reviewService.GetSummaryAsync(venueId));
}
