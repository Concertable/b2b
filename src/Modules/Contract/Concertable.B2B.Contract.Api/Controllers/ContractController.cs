using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.Kernel.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Contract.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class ContractController : ControllerBase
{
    private readonly IContractService contractService;

    public ContractController(IContractService contractService)
    {
        this.contractService = contractService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await contractService.GetByIdAsync(id)
            ?? throw new NotFoundException($"Contract {id} not found");
        return Ok(contract);
    }
}
