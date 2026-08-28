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
    private readonly IMembershipContext membership;

    public ApplicationController(
        IApplicationService applicationService,
        IApplicationResponseMapper mapper,
        IMembershipContext membership)
    {
        this.applicationService = applicationService;
        this.mapper = mapper;
        this.membership = membership;
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpGet("opportunity/{id}")]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse<VenueApplicationActions>>>> GetAllByOpportunityId(int id)
    {
        var result = await applicationService.GetByOpportunityIdAsync(id);
        return (await result.MapAsync(mapper.ToVenueResponsesAsync)).ToOkOrProblem();
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [EnableRateLimiting(RateLimitPolicies.Apply)]
    [HttpPost("{opportunityId}")]
    public async Task<ActionResult<ApplicationResponse<ArtistApplicationActions>>> Apply(
        int opportunityId,
        [FromBody] ApplyRequest request)
    {
        var result = request.PaymentMethodId is not null
            ? await applicationService.ApplyAsync(opportunityId, request.PaymentMethodId, request.ESignature)
            : await applicationService.ApplyAsync(opportunityId, request.ESignature);
        var response = await result.MapAsync(mapper.ToArtistResponseAsync);
        return response.ToCreatedOrProblem(application => $"/api/application/{application.Id}");
    }

    [HttpGet("artist/pending")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse<ArtistApplicationActions>>>> GetPendingForArtist()
    {
        var result = await applicationService.GetPendingForArtistAsync();
        return (await result.MapAsync(mapper.ToArtistResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("artist/recently-denied")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse<ArtistApplicationActions>>>> GetRecentDeniedForArtist()
    {
        var result = await applicationService.GetRecentDeniedForArtistAsync();
        return (await result.MapAsync(mapper.ToArtistResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("venue/current")]
    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse<VenueApplicationActions>>>> GetPendingForCurrentVenue()
    {
        var result = await applicationService.GetPendingForCurrentVenueAsync();
        return (await result.MapAsync(mapper.ToVenueResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("artist/current")]
    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse<ArtistApplicationActions>>>> GetCurrentForCurrentArtist()
    {
        var result = await applicationService.GetCurrentForCurrentArtistAsync();
        return (await result.MapAsync(mapper.ToArtistResponsesAsync)).ToOkOrProblem();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        Func<ApplicationDto, Task<ApplicationResponse>> responseMapper;
        switch (membership.Type)
        {
            case TenantType.Venue:
                responseMapper = async dto => await mapper.ToVenueResponseAsync(dto);
                break;
            case TenantType.Artist:
                responseMapper = async dto => await mapper.ToArtistResponseAsync(dto);
                break;
            default:
                return Forbid();
        }

        var result = await applicationService.GetByIdAsync(id);
        return (await result.MapAsync(responseMapper)).ToOkOrProblem();
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
    public async Task<ActionResult<Checkout>> AcceptCheckout(int applicationId)
    {
        return (await applicationService.AcceptCheckoutAsync(applicationId)).ToOkOrProblem();
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
    public async Task<IActionResult> Reject(int applicationId, CancellationToken ct)
    {
        return (await applicationService.RejectAsync(applicationId, ct)).ToNoContentOrProblem();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/cancel")]
    public async Task<IActionResult> Cancel(int applicationId, CancellationToken ct)
    {
        return (await applicationService.CancelAsync(applicationId, ct)).ToNoContentOrProblem();
    }
}
