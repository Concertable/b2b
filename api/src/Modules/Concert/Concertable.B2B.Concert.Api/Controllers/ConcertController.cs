using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Authorization.Contracts;
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
    private readonly IInvoiceService invoiceService;

    public ConcertController(
        IConcertService concertService,
        IInvoiceService invoiceService)
    {
        this.concertService = concertService;
        this.invoiceService = invoiceService;
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("{id}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(int id)
    {
        return (await concertService.GetDetailsByIdAsync(id))
            .ToOkOrProblem(concert => concert.ToDetailsResponse());
    }

    [HasPermission(TenantPermission.ResourcesShare)]
    [HttpPost("{id:int}/summary-shares")]
    public async Task<ActionResult<ConcertSummaryShare>> ShareSummary(
        int id,
        [FromBody] ShareConcertSummaryRequest request,
        CancellationToken ct) =>
        (await concertService.ShareSummaryAsync(id, request, ct)).ToOkOrProblem();

    [HasPermission(TenantPermission.ResourcesShare)]
    [HttpDelete("{id:int}/summary-shares/{grantId:guid}")]
    public async Task<IActionResult> RevokeSummaryShare(
        int id,
        Guid grantId,
        [FromQuery] long expectedVersion,
        CancellationToken ct) =>
        (await concertService.RevokeSummaryShareAsync(id, grantId, expectedVersion, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.ResourcesShare)]
    [HttpPost("{id:int}/member-assignments")]
    public async Task<IActionResult> AssignMember(
        int id,
        [FromBody] AssignConcertMemberRequest request,
        CancellationToken ct) =>
        (await concertService.AssignMemberAsync(id, request, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.ResourcesShare)]
    [HttpDelete("{id:int}/member-assignments/{membershipId:guid}")]
    public async Task<IActionResult> RemoveMemberAssignment(
        int id,
        Guid membershipId,
        CancellationToken ct) =>
        (await concertService.RemoveMemberAssignmentAsync(id, membershipId, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.OperationsView)]
    [HttpGet("/api/organization/concert/{concertId:int}")]
    public async Task<ActionResult<MyDetailsResponse>> Get(
        int concertId,
        CancellationToken ct) =>
        (await concertService.GetDetailsAsync(concertId, ct))
            .ToOkOrProblem(concert => concert.ToMyDetailsResponse());

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("{id}/contract/pdf")]
    public async Task<ActionResult<FileDownload>> GetContractPdf(int id)
    {
        return (await concertService.GetContractPdfAsync(id))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("{id}/invoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        return (await invoiceService.GetByConcertIdAsync(id))
            .ToOkOrProblem();
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("{id}/invoice/pdf")]
    public async Task<ActionResult<FileDownload>> GetInvoicePdf(int id)
    {
        return (await invoiceService.GetPdfByConcertIdAsync(id))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<MyDetailsResponse>> GetDetailsByApplicationId(int applicationId)
    {
        return (await concertService.GetDetailsByApplicationIdAsync(applicationId))
            .ToOkOrProblem(concert => concert.ToMyDetailsResponse());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("upcoming/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUpcomingByVenueId(int id)
    {
        return Ok((await concertService.GetUpcomingByVenueIdAsync(id)).ToSummaryResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("upcoming/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUpcomingByArtistId(int id)
    {
        return Ok((await concertService.GetUpcomingByArtistIdAsync(id)).ToSummaryResponses());
    }

    [HttpGet("upcoming/venue/current")]
    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<ManagerConcertCard>>> GetUpcomingForCurrentVenue() =>
        (await concertService.GetUpcomingForCurrentVenueAsync()).ToOkOrProblem();

    [HttpGet("upcoming/artist/current")]
    [RequiresBusinessProfile(TenantBusinessProfileKind.Artist)]
    [HasPermission(TenantPermission.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<ManagerConcertCard>>> GetUpcomingForCurrentArtist() =>
        (await concertService.GetUpcomingForCurrentArtistAsync()).ToOkOrProblem();

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("history/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetHistoryByVenueId(int id)
    {
        return Ok((await concertService.GetHistoryByVenueIdAsync(id)).ToSummaryResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("history/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetHistoryByArtistId(int id)
    {
        return Ok((await concertService.GetHistoryByArtistIdAsync(id)).ToSummaryResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("unposted/venue/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByVenueId(int id)
    {
        return Ok((await concertService.GetUnpostedByVenueIdAsync(id)).ToSummaryResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("unposted/artist/{id}")]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByArtistId(int id)
    {
        return Ok((await concertService.GetUnpostedByArtistIdAsync(id)).ToSummaryResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.ConcertsOpsEdit)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ConcertUpdateResponse>> Update(
        int id,
        [FromBody] UpdateConcertRequest request,
        CancellationToken ct)
    {
        return (await concertService.UpdateAsync(id, request, ct)).ToOkOrProblem();
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.ConcertsOpsEdit)]
    [HttpPut("post/{id}")]
    public async Task<IActionResult> Post(
        int id,
        [FromBody] UpdateConcertRequest request,
        CancellationToken ct)
    {
        return (await concertService.PostAsync(id, request, ct)).ToNoContentOrProblem();
    }

    [HasPermission(TenantPermission.ConcertsManage)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        return (await concertService.CancelAsync(id, ct)).ToNoContentOrProblem();
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.ConcertsDeclareDoorRevenue)]
    [HttpPost("{id}/door-revenue")]
    public async Task<IActionResult> DeclareDoorRevenue(
        int id,
        [FromBody] DoorRevenueRequest request,
        CancellationToken ct)
    {
        return (await concertService.DeclareDoorRevenueAsync(id, request.DoorRevenue, ct)).ToNoContentOrProblem();
    }
}
