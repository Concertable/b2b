using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Errors;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[TenantPersona(TenantType.Venue)]
internal sealed class ConcertController : ControllerBase
{
    private readonly IConcertService concertService;
    private readonly ICancelExecutor cancelExecutor;
    private readonly IContractService contractService;
    private readonly IInvoiceService invoiceService;
    private readonly TimeProvider timeProvider;

    public ConcertController(
        IConcertService concertService,
        ICancelExecutor cancelExecutor,
        IContractService contractService,
        IInvoiceService invoiceService,
        TimeProvider timeProvider)
    {
        this.concertService = concertService;
        this.cancelExecutor = cancelExecutor;
        this.contractService = contractService;
        this.invoiceService = invoiceService;
        this.timeProvider = timeProvider;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConcertDetailsResponse>> GetDetailsById(int id)
    {
        return (await concertService.GetDetailsByIdAsync(id))
            .Map(concert => concert.ToDetailsResponse())
            .OrFailure(() => ConcertLookupError.ConcertNotFound(id))
            .ToOkActionResult();
    }

    // Current-user (party) read: tenant-scoped (404 for non-parties), so it carries the party-only
    // action links. No [HasPermission] — both parties read it; the repository stance is the gate,
    // not a role. Mirrors venue's GET /venue/user.
    [HttpGet("user/{id}")]
    public async Task<ActionResult<ConcertDetailsResponse>> GetDetailsForCurrentUser(int id)
    {
        return (await concertService.GetDetailsForCurrentUserAsync(id))
            .Map(concert => concert.ToCurrentUserDetailsResponse(timeProvider.GetUtcNow().UtcDateTime))
            .OrFailure(() => ConcertLookupError.ConcertNotFound(id))
            .ToOkActionResult();
    }

    [HttpGet("{id}/contract/pdf")]
    public async Task<ActionResult<FileDownload>> GetContractPdf(int id)
    {
        return (await contractService.GetPdfByConcertIdAsync(id))
            .OrFailure(() => ConcertLookupError.ContractByConcertNotFound(id))
            .ToActionResult(pdf => File(pdf.Content, pdf.ContentType, pdf.FileName));
    }

    [HttpGet("{id}/invoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        return (await invoiceService.GetByConcertIdAsync(id))
            .OrFailure(() => ConcertLookupError.InvoiceByConcertNotFound(id))
            .ToOkActionResult();
    }

    [HttpGet("{id}/invoice/pdf")]
    public async Task<ActionResult<FileDownload>> GetInvoicePdf(int id)
    {
        return (await invoiceService.GetPdfByConcertIdAsync(id))
            .OrFailure(() => ConcertLookupError.InvoiceByConcertNotFound(id))
            .ToActionResult(pdf => File(pdf.Content, pdf.ContentType, pdf.FileName));
    }

    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<ConcertDetailsResponse>> GetDetailsByApplicationId(int applicationId)
    {
        return (await concertService.GetDetailsByApplicationIdAsync(applicationId))
            .Map(concert => concert.ToCurrentUserDetailsResponse(timeProvider.GetUtcNow().UtcDateTime))
            .OrFailure(() => ConcertLookupError.ConcertByApplicationNotFound(applicationId))
            .ToOkActionResult();
    }

    [HttpGet("upcoming/venue/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUpcomingByVenueId(int id)
    {
        return Ok((await concertService.GetUpcomingByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("upcoming/artist/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUpcomingByArtistId(int id)
    {
        return Ok((await concertService.GetUpcomingByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("history/venue/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetHistoryByVenueId(int id)
    {
        return Ok((await concertService.GetHistoryByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("history/artist/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetHistoryByArtistId(int id)
    {
        return Ok((await concertService.GetHistoryByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("unposted/venue/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUnpostedByVenueId(int id)
    {
        return Ok((await concertService.GetUnpostedByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("unposted/artist/{id}")]
    public async Task<ActionResult<IEnumerable<ConcertSummaryResponse>>> GetUnpostedByArtistId(int id)
    {
        return Ok((await concertService.GetUnpostedByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ConcertUpdateResponse>> Update(int id, [FromBody] UpdateConcertRequest request)
    {
        return (await concertService.UpdateAsync(id, request)).ToOkActionResult();
    }

    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPut("post/{id}")]
    public async Task<IActionResult> Post(int id, [FromBody] UpdateConcertRequest request)
    {
        return (await concertService.PostAsync(id, request)).ToNoContentActionResult();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var result = await cancelExecutor.CancelAsync(id, ct);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);
        return NoContent();
    }

    /// <summary>
    /// Venue declares the external door take (own-site + other ticketers + cash) for an ended,
    /// still-Booked revenue-share gig, gating its settlement. Re-declarable until it settles, then frozen.
    /// </summary>
    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPost("{id}/door-revenue")]
    public async Task<IActionResult> DeclareDoorRevenue(int id, [FromBody] DoorRevenueRequest request)
    {
        return (await concertService.DeclareDoorRevenueAsync(id, request.DoorRevenue)).ToNoContentActionResult();
    }
}
