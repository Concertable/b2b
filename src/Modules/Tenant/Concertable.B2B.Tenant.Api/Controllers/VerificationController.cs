using Concertable.B2B.Tenant.Application.DTOs;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Reunion.AspNetCore.Mvc;

namespace Concertable.B2B.Tenant.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/organization/verification")]
internal sealed class VerificationController : ControllerBase
{
    private readonly IVerificationService verificationService;

    public VerificationController(IVerificationService verificationService)
    {
        this.verificationService = verificationService;
    }

    [HttpGet]
    public async Task<ActionResult<VerificationStatusDto>> Get(CancellationToken ct) =>
        (await verificationService.GetOwnAsync(ct)).ToOkOrNoContent();

    [HttpPost("documents")]
    [HasPermission(SharedPermissions.TenantSettingsEdit)]
    [EnableRateLimiting(RateLimitPolicies.Upload)]
    public async Task<ActionResult<VerificationStatusDto>> SubmitDocuments(
        [FromForm] SubmitVerificationRequest request,
        CancellationToken ct) =>
        (await verificationService.SubmitAsync(request, ct)).ToOkOrProblem();
}
