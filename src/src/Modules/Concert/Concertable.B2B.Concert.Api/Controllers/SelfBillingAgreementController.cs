using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Requests;
using Concertable.B2B.Concert.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

/// <summary>
/// The supplier's own self-billing agreement — a standing, per-tenant compliance fact, reachable by both tenant
/// types (the artist is the supplier for FlatFee/DoorSplit/Versus, the venue for VenueHire). Every read and write
/// is single-owner scoped to the caller's tenant, so there is no id and a caller can only ever act on its own.
/// </summary>
[ApiController]
[Authorize]
[Route("api/self-billing-agreement")]
internal sealed class SelfBillingAgreementController : ControllerBase
{
    private readonly ISelfBillingAgreementService service;
    private readonly TimeProvider timeProvider;

    public SelfBillingAgreementController(ISelfBillingAgreementService service, TimeProvider timeProvider)
    {
        this.service = service;
        this.timeProvider = timeProvider;
    }

    [HttpGet]
    public async Task<ActionResult<SelfBillingAgreementResponse>> GetCurrent()
    {
        var latest = await service.GetLatestAsync();
        return Ok(latest.ToResponse(timeProvider.GetUtcNow().UtcDateTime));
    }

    [HttpPost]
    public async Task<IActionResult> Grant([FromBody] GrantSelfBillingAgreementRequest request)
    {
        await service.GrantAsync(request.ESignature);
        return NoContent();
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> GetPdf()
    {
        var pdf = await service.GetPdfAsync();
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }
}
