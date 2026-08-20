using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Application.DTOs;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
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

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("{id}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(int id)
    {
        return (await concertService.GetDetailsByIdAsync(id))
            .ToOkOrProblem(concert => concert.ToDetailsResponse());
    }

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("/api/organization/concert/{concertId:int}")]
    public async Task<ActionResult<MyDetailsResponse>> Get(
        int concertId,
        CancellationToken ct) =>
        (await concertService.GetDetailsAsync(concertId, ct))
            .ToOkOrProblem(concert => concert.ToMyDetailsResponse());

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("{id}/contract/pdf")]
    public async Task<ActionResult<FileDownload>> GetContractPdf(int id)
    {
        return (await contractService.GetPdfByConcertIdAsync(id))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("{id}/invoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        return (await invoiceService.GetByConcertIdAsync(id))
            .ToOkOrProblem();
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("{id}/invoice/pdf")]
    public async Task<ActionResult<FileDownload>> GetInvoicePdf(int id)
    {
        return (await invoiceService.GetPdfByConcertIdAsync(id))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<MyDetailsResponse>> GetDetailsByApplicationId(int applicationId)
    {
        return (await concertService.GetDetailsByApplicationIdAsync(applicationId))
            .ToOkOrProblem(concert => concert.ToMyDetailsResponse());
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("upcoming/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUpcomingByVenueId(int id)
    {
        return Ok((await concertService.GetUpcomingByVenueIdAsync(id)).ToSummaryResponses());
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("upcoming/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUpcomingByArtistId(int id)
    {
        return Ok((await concertService.GetUpcomingByArtistIdAsync(id)).ToSummaryResponses());
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("history/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetHistoryByVenueId(int id)
    {
        return Ok((await concertService.GetHistoryByVenueIdAsync(id)).ToSummaryResponses());
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("history/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetHistoryByArtistId(int id)
    {
        return Ok((await concertService.GetHistoryByArtistIdAsync(id)).ToSummaryResponses());
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("unposted/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByVenueId(int id)
    {
        return Ok((await concertService.GetUnpostedByVenueIdAsync(id)).ToSummaryResponses());
    }

    [RequiredTenantType(TenantType.Venue)]
    [HttpGet("unposted/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByArtistId(int id)
    {
        return Ok((await concertService.GetUnpostedByArtistIdAsync(id)).ToSummaryResponses());
    }

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ConcertUpdateResponse>> Update(int id, [FromBody] UpdateConcertRequest request)
    {
        return (await concertService.UpdateAsync(id, request)).ToOkOrProblem();
    }

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPut("post/{id}")]
    public async Task<IActionResult> Post(int id, [FromBody] UpdateConcertRequest request)
    {
        return (await concertService.PostAsync(id, request)).ToNoContentOrProblem();
    }

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(VenuePermissions.ApplicationsDecide)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        return (await concertService.CancelAsync(id, ct)).ToNoContentOrProblem();
    }

    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(VenuePermissions.ConcertsManage)]
    [HttpPost("{id}/door-revenue")]
    public async Task<IActionResult> DeclareDoorRevenue(int id, [FromBody] DoorRevenueRequest request)
    {
        return (await concertService.DeclareDoorRevenueAsync(id, request.DoorRevenue)).ToNoContentOrProblem();
    }
}
