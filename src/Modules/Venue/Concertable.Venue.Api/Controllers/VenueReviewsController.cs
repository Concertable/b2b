using Microsoft.AspNetCore.Mvc;

namespace Concertable.Venue.Api.Controllers;

[ApiController]
[Route("api/venues/{venueId}/reviews")]
internal class VenueReviewsController : ControllerBase
{
    private readonly IVenueReviewService reviewService;

    public VenueReviewsController(IVenueReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ReviewSummaryDto>> GetSummary(int venueId) =>
        Ok(await reviewService.GetSummaryAsync(venueId));
}
