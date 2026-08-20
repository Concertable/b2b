using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route($"api/{ArtistController.RouteSegment}/{{artistId:int}}/{RouteSegment}")]
internal sealed class ArtistReviewController : ControllerBase
{
    internal const string RouteSegment = "review";

    private readonly IArtistReviewService reviewService;

    public ArtistReviewController(IArtistReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<IPagination<ReviewDto>>> GetReviews(
        int artistId,
        [FromQuery] PageParams pageParams) =>
        Ok(await reviewService.GetPagedAsync(artistId, pageParams));

    [HttpGet("summary")]
    public async Task<ActionResult<ReviewSummary>> GetSummary(int artistId) =>
        Ok(await reviewService.GetSummaryAsync(artistId));

    [HttpGet($"/api/organization/{ArtistController.RouteSegment}/{RouteSegment}/recent")]
    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<RecentReviewResponse>>> GetRecentForCurrent(
        CancellationToken ct) =>
        (await reviewService.GetRecentForCurrentAsync(5, ct))
            .ToOkOrNoContent(reviews => reviews.ToResponses());
}
