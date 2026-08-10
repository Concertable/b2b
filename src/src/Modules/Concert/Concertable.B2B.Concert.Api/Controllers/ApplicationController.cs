using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Requests;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class ApplicationController : ControllerBase
{
    private readonly IApplicationService applicationService;
    private readonly IContractService contractService;
    private readonly IApplicationResponseMapper mapper;

    public ApplicationController(
        IApplicationService applicationService,
        IContractService contractService,
        IApplicationResponseMapper mapper)
    {
        this.applicationService = applicationService;
        this.contractService = contractService;
        this.mapper = mapper;
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpGet("opportunity/{id}")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetAllByOpportunityId(int id)
    {
        var applications = await applicationService.GetByOpportunityIdAsync(id);
        return Ok(mapper.ToResponses(applications));
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [HttpPost("{opportunityId}")]
    public async Task<ActionResult<ApplicationResponse>> Apply(int opportunityId, [FromBody] ApplyRequest request)
    {
        var result = request.PaymentMethodId is not null
            ? await applicationService.ApplyAsync(opportunityId, request.PaymentMethodId, request.ESignature)
            : await applicationService.ApplyAsync(opportunityId, request.ESignature);
        return result
            .Map(mapper.ToResponse)
            .ToActionResult(application =>
                CreatedAtAction(nameof(GetById), new { id = application.Id }, application));
    }

    [HttpGet("artist/pending")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetPendingForArtist()
    {
        var applications = await applicationService.GetPendingForArtistAsync();
        return Ok(mapper.ToResponses(applications));
    }

    [HttpGet("artist/recently-denied")]
    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetRecentDeniedForArtist()
    {
        var applications = await applicationService.GetRecentDeniedForArtistAsync();
        return Ok(mapper.ToResponses(applications));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        return (await applicationService.GetByIdAsync(id))
            .Map(mapper.ToResponse)
            .ToOkOrProblem();
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
            .ToActionResult(pdf => File(pdf.Content, pdf.ContentType, pdf.FileName));
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
    [HttpPost("opportunity/{opportunityId}/checkout")]
    public async Task<IActionResult> ApplyCheckout(int opportunityId)
    {
        var checkout = await applicationService.ApplyCheckoutAsync(opportunityId);
        return Ok(checkout);
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

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/cancel")]
    public async Task<IActionResult> Cancel(int applicationId, CancellationToken ct)
    {
        return (await applicationService.CancelAsync(applicationId, ct)).ToNoContentOrProblem();
    }

}
