using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequiredTenantType(TenantType.Venue)]
internal sealed class ConcertController : ControllerBase
{
    private readonly IConcertService concertService;
    private readonly IContractService contractService;
    private readonly IInvoiceService invoiceService;
    private readonly TimeProvider timeProvider;

    public ConcertController(
        IConcertService concertService,
        IContractService contractService,
        IInvoiceService invoiceService,
        TimeProvider timeProvider)
    {
        this.concertService = concertService;
        this.contractService = contractService;
        this.invoiceService = invoiceService;
        this.timeProvider = timeProvider;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(int id)
    {
        return Ok((await concertService.GetDetailsByIdAsync(id)).ToDetailsResponse());
    }

    // Current-user (party) read: tenant-scoped (404 for non-parties), so it carries the party-only
    // action links. No [HasPermission] — both parties read it; the repository stance is the gate,
    // not a role. Mirrors venue's GET /venue/user.
    [HttpGet("user/{id}")]
    public async Task<ActionResult<MyDetailsResponse>> GetDetailsForCurrentUser(int id)
    {
        return Ok((await concertService.GetDetailsForCurrentUserAsync(id))
            .ToMyDetailsResponse(timeProvider.GetUtcNow().UtcDateTime));
    }

    [HttpGet("{id}/contract/pdf")]
    public async Task<IActionResult> GetContractPdf(int id)
    {
        var pdf = await contractService.GetPdfByConcertIdAsync(id);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }

    [HttpGet("{id}/invoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        return Ok(await invoiceService.GetByConcertIdAsync(id));
    }

    [HttpGet("{id}/invoice/pdf")]
    public async Task<IActionResult> GetInvoicePdf(int id)
    {
        var pdf = await invoiceService.GetPdfByConcertIdAsync(id);
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }

    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<MyDetailsResponse>> GetDetailsByApplicationId(int applicationId)
    {
        return Ok((await concertService.GetDetailsByApplicationIdAsync(applicationId))
            .ToMyDetailsResponse(timeProvider.GetUtcNow().UtcDateTime));
    }

    [HttpGet("upcoming/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUpcomingByVenueId(int id)
    {
        return Ok((await concertService.GetUpcomingByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("upcoming/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUpcomingByArtistId(int id)
    {
        return Ok((await concertService.GetUpcomingByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("history/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetHistoryByVenueId(int id)
    {
        return Ok((await concertService.GetHistoryByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("history/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetHistoryByArtistId(int id)
    {
        return Ok((await concertService.GetHistoryByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("unposted/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByVenueId(int id)
    {
        return Ok((await concertService.GetUnpostedByVenueIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("unposted/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByArtistId(int id)
    {
        return Ok((await concertService.GetUnpostedByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ConcertUpdateResponse>> Update(int id, [FromBody] UpdateConcertRequest request)
    {
        return Ok(await concertService.UpdateAsync(id, request));
    }

    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPut("post/{id}")]
    public async Task<IActionResult> Post(int id, [FromBody] UpdateConcertRequest request)
    {
        await concertService.PostAsync(id, request);
        return NoContent();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        await concertService.CancelAsync(id, ct);
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
        await concertService.DeclareDoorRevenueAsync(id, request.DoorRevenue);
        return NoContent();
    }
}
