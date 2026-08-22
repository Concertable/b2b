using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Requests;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class ApplicationController : ControllerBase
{
    private readonly IApplicationService applicationService;
    private readonly IContractService contractService;
    private readonly IApplicationResponseMapper mapper;
    private readonly IMembershipContext membership;

    public ApplicationController(
        IApplicationService applicationService,
        IContractService contractService,
        IApplicationResponseMapper mapper,
        IMembershipContext membership)
    {
        this.applicationService = applicationService;
        this.contractService = contractService;
        this.mapper = mapper;
        this.membership = membership;
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpGet("opportunity/{id}")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse<VenueApplicationActions>>>> GetAllByOpportunityId(int id)
    {
        return (await applicationService.GetByOpportunityIdAsync(id))
            .ToOkOrProblem(mapper.ToVenueResponses);
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
        return result.ToCreatedOrProblem(
            mapper.ToArtistResponse,
            application => $"/api/application/{application.Id}");
    }

    [HttpGet("artist/pending")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IEnumerable<ApplicationResponse<ArtistApplicationActions>>>> GetPendingForArtist()
    {
        return (await applicationService.GetPendingForArtistAsync())
            .ToOkOrProblem(mapper.ToArtistResponses);
    }

    [HttpGet("artist/recently-denied")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IEnumerable<ApplicationResponse<ArtistApplicationActions>>>> GetRecentDeniedForArtist()
    {
        return (await applicationService.GetRecentDeniedForArtistAsync())
            .ToOkOrProblem(mapper.ToArtistResponses);
    }

    [HttpGet("venue/current")]
    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IEnumerable<ApplicationResponse<VenueApplicationActions>>>> GetPendingForCurrentVenue()
    {
        return (await applicationService.GetPendingForCurrentVenueAsync())
            .ToOkOrProblem(mapper.ToVenueResponses);
    }

    [HttpGet("artist/current")]
    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IEnumerable<ApplicationResponse<ArtistApplicationActions>>>> GetCurrentForCurrentArtist()
    {
        return (await applicationService.GetCurrentForCurrentArtistAsync())
            .ToOkOrProblem(mapper.ToArtistResponses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        Func<ApplicationDto, ApplicationResponse> responseMapper;
        switch (membership.Type)
        {
            case TenantType.Venue:
                responseMapper = mapper.ToVenueResponse;
                break;
            case TenantType.Artist:
                responseMapper = mapper.ToArtistResponse;
                break;
            default:
                return Forbid();
        }

        return (await applicationService.GetByIdAsync(id))
            .ToOkOrProblem(responseMapper);
    }

    // No [HasPermission]: both parties read (venue + artist), enforced by the two-party tenant filter
    // exactly like GetById — a stranger is filtered out and gets 404, never a probe-able 403.
    [HttpGet("{id}/contract")]
    public async Task<ActionResult<ContractDto>> GetContract(int id)
    {
        return (await contractService.GetByApplicationIdAsync(id))
            .ToOkOrProblem();
    }

    [HttpGet("{id}/contract/pdf")]
    public async Task<ActionResult<FileDownload>> GetContractPdf(int id)
    {
        return (await contractService.GetPdfByApplicationIdAsync(id))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
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

    [HttpGet("{id}/financial-operation")]
    public async Task<ActionResult<FinancialOperation>> GetFinancialOperation(
        int id,
        CancellationToken ct)
    {
        return (await applicationService.GetFinancialOperationAsync(id, ct)).ToOkOr(NotFound);
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/reject")]
    public async Task<IActionResult> Reject(int applicationId)
    {
        return (await applicationService.RejectAsync(applicationId)).ToNoContentOrProblem();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/cancel")]
    public async Task<IActionResult> Cancel(int applicationId, CancellationToken ct)
    {
        return (await applicationService.CancelAsync(applicationId, ct)).ToNoContentOrProblem();
    }

}
