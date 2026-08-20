using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Venue.Api.Mappers;
using Concertable.B2B.Venue.Api.Responses;
using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Venue.Api.Controllers;

[ApiController]
[Route($"api/{VenueController.RouteSegment}/{{venueId:int}}/{RouteSegment}")]
internal sealed class VenueReviewController : ControllerBase
{
    internal const string RouteSegment = "review";

    private readonly IVenueReviewService reviewService;

    public VenueReviewController(IVenueReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<IPagination<ReviewDto>>> GetReviews(
        int venueId,
        [FromQuery] PageParams pageParams) =>
        Ok(await reviewService.GetPagedAsync(venueId, pageParams));

    [HttpGet("summary")]
    public async Task<ActionResult<ReviewSummary>> GetSummary(int venueId) =>
        Ok(await reviewService.GetSummaryAsync(venueId));

    [HttpGet($"/api/organization/{VenueController.RouteSegment}/{RouteSegment}/recent")]
    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<RecentReviewResponse>>> GetRecentForCurrent(
        CancellationToken ct) =>
        (await reviewService.GetRecentForCurrentAsync(5, ct))
            .ToOkOrNoContent(reviews => reviews.ToResponses());
}
