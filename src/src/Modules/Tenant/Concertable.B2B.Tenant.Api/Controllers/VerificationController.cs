using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Mappers;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Admin.Api.Authorization;
using Concertable.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

/// <summary>
/// Tenant self-service verification (own status/submission, under the active-organization surface) and
/// platform-admin review (any tenant's pending queue, approve/reject) on one shared <see cref="IVerificationService"/>.
/// Mirrors <c>VenueController</c>'s shape exactly: one controller, per-action authorization, mixed route
/// prefixes via an absolute override for the organization-scoped actions.
/// </summary>
[ApiController]
[Route($"api/{RouteSegment}")]
internal sealed class VerificationController : ControllerBase
{
    internal const string RouteSegment = "verification";

    private readonly IVerificationService verificationService;

    public VerificationController(IVerificationService verificationService)
    {
        this.verificationService = verificationService;
    }

    [Authorize]
    [HttpGet($"/api/organization/{RouteSegment}")]
    public async Task<ActionResult<VerificationStatusDto>> Get(CancellationToken ct) =>
        (await verificationService.GetStatusAsync(ct)).ToOkOrNoContent();

    [Authorize]
    [HasPermission(SharedPermissions.TenantSettingsEdit)]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    [HttpPost($"/api/organization/{RouteSegment}/documents")]
    public async Task<ActionResult<VerificationStatusDto>> SubmitDocuments(
        [FromForm] SubmitVerificationRequest request,
        CancellationToken ct) =>
        (await verificationService.SubmitAsync(request.ToEvidenceUploads(), ct)).ToOkOrProblem();

    [Admin]
    [HttpGet("pending")]
    public async Task<ActionResult<IPagination<PendingVerificationDto>>> GetPending(
        [FromQuery] PageParams pageParams,
        CancellationToken ct) =>
        Ok(await verificationService.GetPendingAsync(pageParams, ct));

    [Admin]
    [HttpPost("{tenantId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid tenantId, CancellationToken ct) =>
        (await verificationService.ApproveAsync(tenantId, ct)).ToNoContentOrProblem();

    [Admin]
    [HttpPost("{tenantId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid tenantId,
        [FromBody] RejectVerificationRequest request,
        CancellationToken ct) =>
        (await verificationService.RejectAsync(tenantId, request.Reason, ct)).ToNoContentOrProblem();
}
