using Concertable.B2B.Booking.Application.DTOs;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Booking.Api.Controllers;

[ApiController]
[Route("api/application")]
internal sealed class ContractController : ControllerBase
{
    private readonly IContractService contractService;

    public ContractController(IContractService contractService) => this.contractService = contractService;

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("{id}/contract")]
    public async Task<ActionResult<ContractDto>> Get(int id, CancellationToken ct)
    {
        return (await contractService.GetByApplicationIdAsync(id, ct)).ToOkOrProblem();
    }

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("{id}/contract/pdf")]
    public async Task<ActionResult<FileDownload>> GetPdf(int id, CancellationToken ct)
    {
        return (await contractService.GetPdfByApplicationIdAsync(id, ct))
            .ToActionResult(pdf => new ActionResult<FileDownload>(
                File(pdf.Content, pdf.ContentType, pdf.FileName)));
    }
}
