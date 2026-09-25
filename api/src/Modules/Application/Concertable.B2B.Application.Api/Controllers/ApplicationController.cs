using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Application.Application.DTOs;
using Concertable.B2B.Application.Api.Mappers;
using Concertable.B2B.Application.Api.Requests;
using Concertable.B2B.Application.Api.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.B2B.Application.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class ApplicationController : ControllerBase
{
    private readonly IApplicationService applicationService;
    private readonly IApplicationMapper mapper;

    public ApplicationController(
        IApplicationService applicationService,
        IApplicationMapper mapper)
    {
        this.applicationService = applicationService;
        this.mapper = mapper;
    }

    [HasPermission(TenantPermission.ApplicationsDecideName)]
    [HttpGet("opportunity/{id}")]
    [HasPermission(TenantPermission.TermsReadName)]
    public async Task<ActionResult<IReadOnlyList<ApplicationProposalResponse>>> GetAllByOpportunityId(
        int id,
        CancellationToken ct)
    {
        var result = await applicationService.GetByOpportunityIdAsync(id, ct);
        return (await result.MapAsync(dtos => mapper.ToProposalResponsesAsync(dtos, ct))).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsSubmitName)]
    [EnableRateLimiting(RateLimitPolicies.Apply)]
    [HttpPost("{opportunityId}")]
    public async Task<ActionResult<ApplicationProposalResponse>> Apply(
        int opportunityId,
        [FromBody] ApplyRequest request,
        CancellationToken ct)
    {
        var result = await applicationService.ApplyAsync(opportunityId, request.ESignature, ct);
        var response = await result.MapAsync(dto => mapper.ToProposalResponseAsync(dto, ct));
        return response.ToCreatedOrProblem(application => $"/api/application/{application.Id}/proposal");
    }

    [HttpGet("artist/pending")]
    [HasPermission(TenantPermission.ApplicationsSubmitName)]
    [HasPermission(TenantPermission.TermsReadName)]
    public async Task<ActionResult<IReadOnlyList<ApplicationProposalResponse>>> GetPendingForArtist(CancellationToken ct)
    {
        var result = await applicationService.GetPendingForArtistAsync(ct);
        return (await result.MapAsync(dtos => mapper.ToProposalResponsesAsync(dtos, ct))).ToOkOrProblem();
    }

    [HttpGet("artist/recently-denied")]
    [HasPermission(TenantPermission.ApplicationsSubmitName)]
    [HasPermission(TenantPermission.TermsReadName)]
    public async Task<ActionResult<IReadOnlyList<ApplicationProposalResponse>>> GetRecentDeniedForArtist(
        CancellationToken ct)
    {
        var result = await applicationService.GetRecentDeniedForArtistAsync(ct);
        return (await result.MapAsync(dtos => mapper.ToProposalResponsesAsync(dtos, ct))).ToOkOrProblem();
    }

    [HttpGet("venue/current")]
    [RequiresBusinessActivity(TenantBusinessActivityKind.VenueOperator)]
    [HasPermission(TenantPermission.OperationsViewName)]
    public async Task<ActionResult<IReadOnlyList<ApplicationSummaryResponse>>> GetPendingForCurrentVenue(
        CancellationToken ct)
    {
        var result = await applicationService.GetPendingForCurrentVenueAsync(ct);
        return (await result.MapAsync(dtos => mapper.ToSummaryResponsesAsync(dtos, ct))).ToOkOrProblem();
    }

    [HttpGet("artist/current")]
    [RequiresBusinessActivity(TenantBusinessActivityKind.Artist)]
    [HasPermission(TenantPermission.OperationsViewName)]
    public async Task<ActionResult<IReadOnlyList<ApplicationSummaryResponse>>> GetCurrentForCurrentArtist(
        CancellationToken ct)
    {
        var result = await applicationService.GetCurrentForCurrentArtistAsync(ct);
        return (await result.MapAsync(dtos => mapper.ToSummaryResponsesAsync(dtos, ct))).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.OperationsViewName)]
    [HttpGet("{id:int}/summary")]
    public async Task<ActionResult<ApplicationSummaryResponse>> GetSummary(int id, CancellationToken ct)
    {
        var result = await applicationService.GetSummaryAsync(id, ct);
        return (await result.MapAsync(dto => mapper.ToSummaryResponseAsync(dto, ct))).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.TermsReadName)]
    [HttpGet("{id:int}/proposal")]
    public async Task<ActionResult<ApplicationProposalResponse>> GetProposal(int id, CancellationToken ct)
    {
        var result = await applicationService.GetProposalAsync(id, ct);
        return (await result.MapAsync(dto => mapper.ToProposalResponseAsync(dto, ct))).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsSubmitName)]
    [HttpGet("opportunity/{opportunityId}/eligibility")]
    public async Task<ActionResult<bool>> CanApply(int opportunityId)
    {
        return Ok(await applicationService.CanApplyAsync(opportunityId));
    }

    [HasPermission(TenantPermission.ApplicationsDecideName)]
    [HttpGet("{applicationId}/eligibility")]
    public async Task<ActionResult<bool>> CanAccept(int applicationId)
    {
        return Ok(await applicationService.CanAcceptAsync(applicationId));
    }

    [HasPermission(TenantPermission.ApplicationsSubmitName)]
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    [HttpPost("opportunity/{opportunityId}/checkout")]
    public async Task<ActionResult<Checkout>> ApplyCheckout(int opportunityId)
    {
        return (await applicationService.ApplyCheckoutAsync(opportunityId)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecideName)]
    [HttpPost("{applicationId}/checkout")]
    public async Task<ActionResult<Checkout>> AcceptCheckout(int applicationId)
    {
        return (await applicationService.AcceptCheckoutAsync(applicationId)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecideName)]
    [HttpPost("{applicationId}/accept")]
    public async Task<IActionResult> Accept(
        int applicationId,
        [FromBody] AcceptRequest request,
        CancellationToken ct)
    {
        return (await applicationService.AcceptAsync(
            applicationId,
            request.ESignature,
            ct)).ToNoContentOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsSubmitName)]
    [HttpPost("{applicationId}/withdraw")]
    public async Task<IActionResult> Withdraw(int applicationId, CancellationToken ct)
    {
        return (await applicationService.WithdrawAsync(applicationId, ct)).ToNoContentOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecideName)]
    [HttpPost("{applicationId}/reject")]
    public async Task<IActionResult> Reject(int applicationId, CancellationToken ct)
    {
        return (await applicationService.RejectAsync(applicationId, ct)).ToNoContentOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecideName)]
    [HttpPost("{applicationId}/cancel")]
    public async Task<IActionResult> Cancel(int applicationId, CancellationToken ct)
    {
        return (await applicationService.CancelAsync(applicationId, ct)).ToNoContentOrProblem();
    }
}
