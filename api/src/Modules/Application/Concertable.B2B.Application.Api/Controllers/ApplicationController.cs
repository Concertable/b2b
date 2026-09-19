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

    [HasPermission(TenantPermission.ApplicationsDecide)]
    [HttpGet("opportunity/{id}")]
    [HasPermission(TenantPermission.TermsRead)]
    public async Task<ActionResult<IReadOnlyList<ApplicationProposalResponse>>> GetAllByOpportunityId(int id)
    {
        var result = await applicationService.GetByOpportunityIdAsync(id);
        return (await result.MapAsync(mapper.ToProposalResponsesAsync)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsSubmit)]
    [EnableRateLimiting(RateLimitPolicies.Apply)]
    [HttpPost("{opportunityId}")]
    public async Task<ActionResult<ApplicationProposalResponse>> Apply(
        int opportunityId,
        [FromBody] ApplyRequest request,
        CancellationToken ct)
    {
        var result = await applicationService.ApplyAsync(opportunityId, request.ESignature, ct);
        var response = await result.MapAsync(mapper.ToProposalResponseAsync);
        return response.ToCreatedOrProblem(application => $"/api/application/{application.Id}/proposal");
    }

    [HttpGet("artist/pending")]
    [HasPermission(TenantPermission.ApplicationsSubmit)]
    [HasPermission(TenantPermission.TermsRead)]
    public async Task<ActionResult<IReadOnlyList<ApplicationProposalResponse>>> GetPendingForArtist()
    {
        var result = await applicationService.GetPendingForArtistAsync();
        return (await result.MapAsync(mapper.ToProposalResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("artist/recently-denied")]
    [HasPermission(TenantPermission.ApplicationsSubmit)]
    [HasPermission(TenantPermission.TermsRead)]
    public async Task<ActionResult<IReadOnlyList<ApplicationProposalResponse>>> GetRecentDeniedForArtist()
    {
        var result = await applicationService.GetRecentDeniedForArtistAsync();
        return (await result.MapAsync(mapper.ToProposalResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("venue/current")]
    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<ApplicationSummaryResponse>>> GetPendingForCurrentVenue()
    {
        var result = await applicationService.GetPendingForCurrentVenueAsync();
        return (await result.MapAsync(mapper.ToSummaryResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("artist/current")]
    [RequiresBusinessProfile(TenantBusinessProfileKind.Artist)]
    [HasPermission(TenantPermission.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<ApplicationSummaryResponse>>> GetCurrentForCurrentArtist()
    {
        var result = await applicationService.GetCurrentForCurrentArtistAsync();
        return (await result.MapAsync(mapper.ToSummaryResponsesAsync)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.OperationsView)]
    [HttpGet("{id:int}/summary")]
    public async Task<ActionResult<ApplicationSummaryResponse>> GetSummary(int id)
    {
        var result = await applicationService.GetSummaryAsync(id);
        return (await result.MapAsync(mapper.ToSummaryResponseAsync)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.TermsRead)]
    [HttpGet("{id:int}/proposal")]
    public async Task<ActionResult<ApplicationProposalResponse>> GetProposal(int id)
    {
        var result = await applicationService.GetProposalAsync(id);
        return (await result.MapAsync(mapper.ToProposalResponseAsync)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsSubmit)]
    [HttpGet("opportunity/{opportunityId}/eligibility")]
    public async Task<ActionResult<bool>> CanApply(int opportunityId)
    {
        return Ok(await applicationService.CanApplyAsync(opportunityId));
    }

    [HasPermission(TenantPermission.ApplicationsDecide)]
    [HttpGet("{applicationId}/eligibility")]
    public async Task<ActionResult<bool>> CanAccept(int applicationId)
    {
        return Ok(await applicationService.CanAcceptAsync(applicationId));
    }

    [HasPermission(TenantPermission.ApplicationsSubmit)]
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    [HttpPost("opportunity/{opportunityId}/checkout")]
    public async Task<ActionResult<Checkout>> ApplyCheckout(int opportunityId)
    {
        return (await applicationService.ApplyCheckoutAsync(opportunityId)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecide)]
    [HttpPost("{applicationId}/checkout")]
    public async Task<ActionResult<Checkout>> AcceptCheckout(int applicationId)
    {
        return (await applicationService.AcceptCheckoutAsync(applicationId)).ToOkOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecide)]
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

    [HasPermission(TenantPermission.ApplicationsSubmit)]
    [HttpPost("{applicationId}/withdraw")]
    public async Task<IActionResult> Withdraw(int applicationId, CancellationToken ct)
    {
        return (await applicationService.WithdrawAsync(applicationId, ct)).ToNoContentOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecide)]
    [HttpPost("{applicationId}/reject")]
    public async Task<IActionResult> Reject(int applicationId, CancellationToken ct)
    {
        return (await applicationService.RejectAsync(applicationId, ct)).ToNoContentOrProblem();
    }

    [HasPermission(TenantPermission.ApplicationsDecide)]
    [HttpPost("{applicationId}/cancel")]
    public async Task<IActionResult> Cancel(int applicationId, CancellationToken ct)
    {
        return (await applicationService.CancelAsync(applicationId, ct)).ToNoContentOrProblem();
    }
}
