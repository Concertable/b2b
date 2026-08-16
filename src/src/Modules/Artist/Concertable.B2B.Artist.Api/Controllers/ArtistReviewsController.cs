using Concertable.Contracts;
using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route("api/artists")]
internal sealed class ArtistReviewsController : ControllerBase
{
    private readonly IArtistReviewService reviewService;

    public ArtistReviewsController(IArtistReviewService reviewService)
    {
        this.reviewService = reviewService;
    }

    [HttpGet("{artistId}/reviews")]
    public async Task<ActionResult<IPagination<ReviewDto>>> GetReviews(int artistId, [FromQuery] PageParams pageParams) =>
        Ok(await reviewService.GetPagedAsync(artistId, pageParams));

    [HttpGet("{artistId}/reviews/summary")]
    public async Task<ActionResult<ReviewSummary>> GetSummary(int artistId) =>
        Ok(await reviewService.GetSummaryAsync(artistId));

    [HttpGet("current/reviews/recent")]
    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<RecentReviewResponse>>> GetRecentForCurrent(
        CancellationToken ct) =>
        Ok((await reviewService.GetRecentForCurrentAsync(5, ct)).ToResponses());
}
