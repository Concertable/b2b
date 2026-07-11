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
    private readonly IApplicationValidator applicationValidator;
    private readonly IBookingAgreementService agreementService;
    private readonly IApplicationResponseMapper mapper;

    public ApplicationController(
        IApplicationService applicationService,
        IApplicationValidator applicationValidator,
        IBookingAgreementService agreementService,
        IApplicationResponseMapper mapper)
    {
        this.applicationService = applicationService;
        this.applicationValidator = applicationValidator;
        this.agreementService = agreementService;
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
    public async Task<IActionResult> Apply(int opportunityId, [FromBody] ApplyRequest request)
    {
        var application = request.PaymentMethodId is not null
            ? await applicationService.ApplyAsync(opportunityId, request.PaymentMethodId, request.ESignature)
            : await applicationService.ApplyAsync(opportunityId, request.ESignature);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, mapper.ToResponse(application));
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
        var application = await applicationService.GetByIdAsync(id);
        return Ok(mapper.ToResponse(application));
    }

    // No [HasPermission]: both parties read (venue + artist), enforced by the two-party tenant filter
    // exactly like GetById — a stranger is filtered out and gets 404, never a probe-able 403.
    [HttpGet("{id}/agreement")]
    public async Task<ActionResult<BookingAgreementDto>> GetAgreement(int id)
    {
        var agreement = await agreementService.GetByApplicationIdAsync(id);
        return Ok(agreement);
    }

    [HttpGet("{id}/agreement/pdf")]
    public async Task<IActionResult> GetAgreementPdf(int id)
    {
        var pdf = await agreementService.GetPdfByApplicationIdAsync(id);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [HttpGet("opportunity/{opportunityId}/eligibility")]
    public async Task<ActionResult<bool>> CanApply(int opportunityId)
    {
        var result = await applicationValidator.CanApplyAsync(opportunityId);
        return Ok(result.IsSuccess);
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpGet("{applicationId}/eligibility")]
    public async Task<ActionResult<bool>> CanAccept(int applicationId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);
        return Ok(result.IsSuccess);
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
    public async Task<IActionResult> Accept(int applicationId, [FromBody] AcceptRequest request)
    {
        await applicationService.AcceptAsync(applicationId, request.PaymentMethodId, request.ESignature);
        return NoContent();
    }

    [HasPermission(ArtistPermissions.ApplicationsSubmit)]
    [HttpPost("{applicationId}/withdraw")]
    public async Task<IActionResult> Withdraw(int applicationId)
    {
        await applicationService.WithdrawAsync(applicationId);
        return NoContent();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/reject")]
    public async Task<IActionResult> Reject(int applicationId)
    {
        await applicationService.RejectAsync(applicationId);
        return NoContent();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{applicationId}/cancel")]
    public async Task<IActionResult> Cancel(int applicationId)
    {
        await applicationService.CancelAsync(applicationId);
        return NoContent();
    }

}
