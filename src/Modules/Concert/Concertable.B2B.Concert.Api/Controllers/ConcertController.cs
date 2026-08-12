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

    public ConcertController(
        IConcertService concertService,
        IContractService contractService,
        IInvoiceService invoiceService)
    {
        this.concertService = concertService;
        this.contractService = contractService;
        this.invoiceService = invoiceService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(int id)
    {
        return (await concertService.GetDetailsByIdAsync(id))
            .Map(concert => concert.ToDetailsResponse())
            .ToOkOrProblem();
    }

    [HttpGet("user/{id}")]
    public async Task<ActionResult<MyDetailsResponse>> GetDetailsForCurrentUser(int id)
    {
        return (await concertService.GetDetailsForCurrentUserAsync(id))
            .Map(concert => concert.ToMyDetailsResponse())
            .ToOkOrProblem();
    }

    [HttpGet("{id}/contract/pdf")]
    public async Task<ActionResult<FileDownload>> GetContractPdf(int id)
    {
        return (await contractService.GetPdfByConcertIdAsync(id))
            .ToActionResult(pdf => File(pdf.Content, pdf.ContentType, pdf.FileName));
    }

    [HttpGet("{id}/invoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        return (await invoiceService.GetByConcertIdAsync(id))
            .ToOkOrProblem();
    }

    [HttpGet("{id}/invoice/pdf")]
    public async Task<ActionResult<FileDownload>> GetInvoicePdf(int id)
    {
        return (await invoiceService.GetPdfByConcertIdAsync(id))
            .ToActionResult(pdf => File(pdf.Content, pdf.ContentType, pdf.FileName));
    }

    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<MyDetailsResponse>> GetDetailsByApplicationId(int applicationId)
    {
        return (await concertService.GetDetailsByApplicationIdAsync(applicationId))
            .Map(concert => concert.ToMyDetailsResponse())
            .ToOkOrProblem();
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
        return (await concertService.UpdateAsync(id, request)).ToOkOrProblem();
    }

    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPut("post/{id}")]
    public async Task<IActionResult> Post(int id, [FromBody] UpdateConcertRequest request)
    {
        return (await concertService.PostAsync(id, request)).ToNoContentOrProblem();
    }

    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        return (await concertService.CancelAsync(id, ct)).ToNoContentOrProblem();
    }

    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPost("{id}/door-revenue")]
    public async Task<IActionResult> DeclareDoorRevenue(int id, [FromBody] DoorRevenueRequest request)
    {
        return (await concertService.DeclareDoorRevenueAsync(id, request.DoorRevenue)).ToNoContentOrProblem();
    }
}
