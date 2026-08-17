using Concertable.B2B.Booking.Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Booking.Api.Controllers;

[ApiController]
[Route("api/application")]
internal sealed class ContractController : ControllerBase
{
    private readonly IContractService contracts;

    public ContractController(IContractService contracts) => this.contracts = contracts;

    [HttpGet("{id}/contract")]
    public async Task<ActionResult<ContractDto>> Get(int id, CancellationToken ct)
    {
        return (await contracts.GetByApplicationIdAsync(id, ct)).ToOkOrProblem();
    }

    [HttpGet("{id}/contract/pdf")]
    public async Task<ActionResult<FileDownload>> GetPdf(int id, CancellationToken ct)
    {
        return (await contracts.GetPdfByApplicationIdAsync(id, ct))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
    }
}
