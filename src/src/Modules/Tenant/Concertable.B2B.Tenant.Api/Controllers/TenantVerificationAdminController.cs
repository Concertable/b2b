using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Admin.Api.Authorization;
using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

/// <summary>
/// Platform review of tenant verification submissions. Gated on <see cref="AdminAttribute"/>, exactly like
/// <c>VenueController</c>'s existing approve/pending-approval and <c>ModerationController</c> — domain-specific
/// admin actions live in their owning module, never centralized in <c>Concertable.B2B.Admin.Api</c>, which owns
/// only the platform admin roster/invite surface.
/// </summary>
[ApiController]
[Route("api/tenant/verification")]
[Admin]
internal sealed class TenantVerificationAdminController : ControllerBase
{
    private readonly IVerificationAdminService adminService;

    public TenantVerificationAdminController(IVerificationAdminService adminService)
    {
        this.adminService = adminService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IPagination<PendingVerificationDto>>> GetPending(
        [FromQuery] PageParams pageParams,
        CancellationToken ct) =>
        Ok(await adminService.GetPendingAsync(pageParams, ct));

    [HttpPost("{tenantId:guid}/approve")]
    public async Task<IActionResult> Approve(Guid tenantId, CancellationToken ct) =>
        (await adminService.ApproveAsync(tenantId, ct)).ToNoContentOrProblem();

    [HttpPost("{tenantId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid tenantId,
        [FromBody] RejectVerificationRequest request,
        CancellationToken ct) =>
        (await adminService.RejectAsync(tenantId, request.Reason, ct)).ToNoContentOrProblem();
}
