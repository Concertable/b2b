using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[TenantPersona(TenantType.Venue)]
internal sealed class ConcertController : ControllerBase
{
    private readonly IConcertService concertService;
    private readonly IConcertWorkflowModule concertWorkflowModule;
    private readonly IBookingAgreementService agreementService;

    public ConcertController(
        IConcertService concertService,
        IConcertWorkflowModule concertWorkflowModule,
        IBookingAgreementService agreementService)
    {
        this.concertService = concertService;
        this.concertWorkflowModule = concertWorkflowModule;
        this.agreementService = agreementService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConcertDetailsResponse>> GetDetailsById(int id)
    {
        return Ok((await concertService.GetDetailsByIdAsync(id)).ToDetailsResponse());
    }

    [HttpGet("{id}/agreement/pdf")]
    public async Task<IActionResult> GetAgreementPdf(int id)
    {
        var pdf = await agreementService.GetPdfByConcertIdAsync(id);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    [HttpGet("application/{applicationId}")]
    public async Task<ActionResult<ConcertDetailsResponse>> GetDetailsByApplicationId(int applicationId)
    {
        return Ok((await concertService.GetDetailsByApplicationIdAsync(applicationId)).ToDetailsResponse());
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
        await concertWorkflowModule.CancelAsync(id, ct);
        return NoContent();
    }
}
