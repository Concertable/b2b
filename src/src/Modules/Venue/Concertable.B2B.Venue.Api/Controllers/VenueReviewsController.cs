using Concertable.Contracts;
using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route("api/venues")]
internal sealed class VenueReviewsController : ControllerBase
{
    private readonly IVenueReviewService reviewService;

    public VenueReviewsController(IVenueReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet("{venueId}/reviews")]
    public async Task<ActionResult<IPagination<ReviewDto>>> GetReviews(int venueId, [FromQuery] PageParams pageParams) =>
        Ok(await reviewService.GetPagedAsync(venueId, pageParams));

    [HttpGet("{venueId}/reviews/summary")]
    public async Task<ActionResult<ReviewSummary>> GetSummary(int venueId) =>
        Ok(await reviewService.GetSummaryAsync(venueId));

    [HttpGet("current/reviews/recent")]
    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<RecentReviewResponse>>> GetRecentForCurrent(
        CancellationToken ct) =>
        (await reviewService.GetRecentForCurrentAsync(5, ct))
            .ToOkOrProblem(reviews => reviews.ToResponses());
}
