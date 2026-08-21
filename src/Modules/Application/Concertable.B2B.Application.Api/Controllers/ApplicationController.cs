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
    private readonly IApplicationResponseMapper mapper;

    public ApplicationController(
        IApplicationService applicationService,
        IApplicationResponseMapper mapper)
    {
        this.applicationService = applicationService;
        this.mapper = mapper;
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpGet("opportunity/{id}")]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse>>> GetAllByOpportunityId(int id)
    {
        var result = await applicationService.GetByOpportunityIdAsync(id);
        return (await result.MapAsync(mapper.ToResponsesAsync)).ToOkOrProblem();
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [EnableRateLimiting(RateLimitPolicies.Apply)]
    [HttpPost("{opportunityId}")]
    public async Task<ActionResult<ApplicationResponse>> Apply(int opportunityId, [FromBody] ApplyRequest request)
    {
        var result = request.PaymentMethodId is not null
            ? await applicationService.ApplyAsync(opportunityId, request.PaymentMethodId, request.ESignature)
            : await applicationService.ApplyAsync(opportunityId, request.ESignature);
        var response = await result.MapAsync(mapper.ToResponseAsync);
        return response.ToCreatedOrProblem(application => $"/api/application/{application.Id}");
    }

    [HttpGet("artist/pending")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse>>> GetPendingForArtist()
    {
        var result = await applicationService.GetPendingForArtistAsync();
        return (await result.MapAsync(mapper.ToResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("artist/recently-denied")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse>>> GetRecentDeniedForArtist()
    {
        var result = await applicationService.GetRecentDeniedForArtistAsync();
        return (await result.MapAsync(mapper.ToResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        var result = await applicationService.GetByIdAsync(id);
        return (await result.MapAsync(mapper.ToResponseAsync)).ToOkOrProblem();
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [HttpGet("opportunity/{opportunityId}/eligibility")]
    public async Task<ActionResult<bool>> CanApply(int opportunityId)
    {
        return Ok(await applicationService.CanApplyAsync(opportunityId));
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpGet("{applicationId}/eligibility")]
    public async Task<ActionResult<bool>> CanAccept(int applicationId)
    {
        return Ok(await applicationService.CanAcceptAsync(applicationId));
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [EnableRateLimiting(RateLimitPolicies.Checkout)]
    [HttpPost("opportunity/{opportunityId}/checkout")]
    public async Task<ActionResult<Checkout>> ApplyCheckout(int opportunityId)
    {
        return (await applicationService.ApplyCheckoutAsync(opportunityId)).ToOkOrProblem();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/checkout")]
    public async Task<IActionResult> AcceptCheckout(int applicationId)
    {
        var checkout = await applicationService.AcceptCheckoutAsync(applicationId);
        return Ok(checkout);
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/accept")]
    public async Task<IActionResult> Accept(
        int applicationId,
        [FromBody] AcceptRequest request,
        CancellationToken ct)
    {
        return (await applicationService.AcceptAsync(
            applicationId,
            request.PaymentMethodId,
            request.ESignature,
            ct)).ToNoContentOrProblem();
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [HttpPost("{applicationId}/withdraw")]
    public async Task<IActionResult> Withdraw(int applicationId, CancellationToken ct)
    {
        return (await applicationService.WithdrawAsync(applicationId, ct)).ToNoContentOrProblem();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/reject")]
    public async Task<IActionResult> Reject(int applicationId)
    {
        return (await applicationService.RejectAsync(applicationId)).ToNoContentOrProblem();
    }
}
