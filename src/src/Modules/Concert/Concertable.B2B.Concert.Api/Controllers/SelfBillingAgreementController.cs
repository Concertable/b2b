using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Requests;
using Concertable.B2B.Concert.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/self-billing-agreement")]
internal sealed class SelfBillingAgreementController : ControllerBase
{
    private readonly ISelfBillingAgreementService service;

    public SelfBillingAgreementController(ISelfBillingAgreementService service)
    {
        this.service = service;
    }

    [HttpGet]
    public async Task<ActionResult<SelfBillingAgreementResponse>> GetCurrent()
    {
        var status = await service.GetStatusAsync();
        return Ok(status.ToResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Grant([FromBody] GrantSelfBillingAgreementRequest request)
    {
        return (await service.GrantAsync(request.ESignature)).ToNoContentOrProblem();
    }

    [HttpGet("pdf")]
    public async Task<IActionResult> GetPdf()
    {
        var pdf = await service.GetPdfAsync();
        return File(pdf.Content, pdf.ContentType, pdf.FileName);
    }
}
