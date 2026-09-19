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

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PublishedConcertResponse>> GetPublished(int id, CancellationToken ct) =>
        (await concertService.GetPublishedAsync(id, ct))
            .ToOkOrProblem(concert => concert.ToResponse());

    [HasPermission(TenantPermission.ResourcesShareName)]
    [HttpPost("{id:int}/summary-shares")]
    public async Task<ActionResult<ConcertSummaryShare>> ShareSummary(
        int id,
        [FromBody] ShareConcertSummaryRequest request,
        CancellationToken ct) =>
        (await concertService.ShareSummaryAsync(id, request, ct)).ToOkOrProblem();

    [HasPermission(TenantPermission.ResourcesShareName)]
    [HttpDelete("{id:int}/summary-shares/{grantId:guid}")]
    public async Task<IActionResult> RevokeSummaryShare(
        int id,
        Guid grantId,
        [FromQuery] long expectedVersion,
        CancellationToken ct) =>
        (await concertService.RevokeSummaryShareAsync(id, grantId, expectedVersion, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.ResourcesShareName)]
    [HttpPost("{id:int}/member-assignments")]
    public async Task<IActionResult> AssignMember(
        int id,
        [FromBody] AssignConcertMemberRequest request,
        CancellationToken ct) =>
        (await concertService.AssignMemberAsync(id, request, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.ResourcesShareName)]
    [HttpDelete("{id:int}/member-assignments/{membershipId:guid}")]
    public async Task<IActionResult> RemoveMemberAssignment(
        int id,
        Guid membershipId,
        CancellationToken ct) =>
        (await concertService.RemoveMemberAssignmentAsync(id, membershipId, ct)).ToNoContentOrProblem();

    [HasPermission(TenantPermission.OperationsViewName)]
    [HttpGet("{id:int}/summary")]
    public async Task<ActionResult<SummaryResponse>> GetSummary(int id, CancellationToken ct) =>
        (await concertService.GetSummaryAsync(id, ct))
            .ToOkOrProblem(concert => concert.ToResponse());

    [HasPermission(TenantPermission.OperationsViewName)]
    [HttpGet("{id:int}/operations")]
    public async Task<ActionResult<OperationsResponse>> GetOperations(int id, CancellationToken ct) =>
        (await concertService.GetOperationsAsync(id, ct))
            .ToOkOrProblem(concert => concert.ToResponse());

    [HasPermission(TenantPermission.SettlementViewName)]
    [HttpGet("{id:int}/finance")]
    public async Task<ActionResult<FinanceResponse>> GetFinance(int id, CancellationToken ct) =>
        (await concertService.GetFinanceAsync(id, ct))
            .ToOkOrProblem(concert => concert.ToResponse());

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.OperationsViewName)]
    [HttpGet("drafts/current")]
    public async Task<ActionResult<IReadOnlyList<ConcertDraftReference>>> GetDraftsForCurrentVenue(
        CancellationToken ct) =>
        (await concertService.GetDraftsForCurrentVenueAsync(ct)).ToOkOrProblem();

    [HasPermission(TenantPermission.SettlementViewName)]
    [HttpGet("{id}/invoice")]
    public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
    {
        return (await invoiceService.GetByConcertIdAsync(id))
            .ToOkOrProblem();
    }

    [HasPermission(TenantPermission.SettlementViewName)]
    [HttpGet("{id}/invoice/pdf")]
    public async Task<ActionResult<FileDownload>> GetInvoicePdf(int id)
    {
        return (await invoiceService.GetPdfByConcertIdAsync(id))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
    }

    [HttpGet("upcoming/venue/{id}")]
    public async Task<ActionResult<IEnumerable<PublishedConcertResponse>>> GetUpcomingByVenueId(
        int id,
        CancellationToken ct)
    {
        return Ok((await concertService.GetUpcomingByVenueIdAsync(id, ct)).ToResponses());
    }

    [HttpGet("upcoming/artist/{id}")]
    public async Task<ActionResult<IEnumerable<PublishedConcertResponse>>> GetUpcomingByArtistId(
        int id,
        CancellationToken ct)
    {
        return Ok((await concertService.GetUpcomingByArtistIdAsync(id, ct)).ToResponses());
    }

    [HttpGet("upcoming/venue/current")]
    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.OperationsViewName)]
    public async Task<ActionResult<IReadOnlyList<ManagerConcertCard>>> GetUpcomingForCurrentVenue() =>
        (await concertService.GetUpcomingForCurrentVenueAsync()).ToOkOrProblem();

    [HttpGet("upcoming/artist/current")]
    [RequiresBusinessProfile(TenantBusinessProfileKind.Artist)]
    [HasPermission(TenantPermission.OperationsViewName)]
    public async Task<ActionResult<IReadOnlyList<ManagerConcertCard>>> GetUpcomingForCurrentArtist() =>
        (await concertService.GetUpcomingForCurrentArtistAsync()).ToOkOrProblem();

    [HttpGet("history/venue/{id}")]
    public async Task<ActionResult<IEnumerable<PublishedConcertResponse>>> GetHistoryByVenueId(
        int id,
        CancellationToken ct)
    {
        return Ok((await concertService.GetHistoryByVenueIdAsync(id, ct)).ToResponses());
    }

    [HttpGet("history/artist/{id}")]
    public async Task<ActionResult<IEnumerable<PublishedConcertResponse>>> GetHistoryByArtistId(
        int id,
        CancellationToken ct)
    {
        return Ok((await concertService.GetHistoryByArtistIdAsync(id, ct)).ToResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("unposted/venue/{id}")]
    [HasPermission(TenantPermission.OperationsViewName)]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByVenueId(
        int id,
        CancellationToken ct)
    {
        return Ok((await concertService.GetUnpostedByVenueIdAsync(id, ct)).ToSummaryResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HttpGet("unposted/artist/{id}")]
    [HasPermission(TenantPermission.OperationsViewName)]
    public async Task<ActionResult<IEnumerable<SummaryResponse>>> GetUnpostedByArtistId(
        int id,
        CancellationToken ct)
    {
        return Ok((await concertService.GetUnpostedByArtistIdAsync(id, ct)).ToSummaryResponses());
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.ConcertsOpsEditName)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ConcertUpdateResponse>> Update(
        int id,
        [FromBody] UpdateConcertRequest request,
        CancellationToken ct)
    {
        return (await concertService.UpdateAsync(id, request, ct)).ToOkOrProblem();
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.ConcertsOpsEditName)]
    [HttpPut("post/{id}")]
    public async Task<IActionResult> Post(
        int id,
        [FromBody] UpdateConcertRequest request,
        CancellationToken ct)
    {
        return (await concertService.PostAsync(id, request, ct)).ToNoContentOrProblem();
    }

    [HasPermission(TenantPermission.ConcertsManageName)]
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        return (await concertService.CancelAsync(id, ct)).ToNoContentOrProblem();
    }

    [RequiresBusinessProfile(TenantBusinessProfileKind.VenueOperator)]
    [HasPermission(TenantPermission.ConcertsDeclareDoorRevenueName)]
    [HttpPost("{id}/door-revenue")]
    public async Task<IActionResult> DeclareDoorRevenue(
        int id,
        [FromBody] DoorRevenueRequest request,
        CancellationToken ct)
    {
        return (await concertService.DeclareDoorRevenueAsync(id, request.DoorRevenue, ct)).ToNoContentOrProblem();
    }
}
