using Concertable.Contract.Application.Interfaces;
using Concertable.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Contract.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class ContractController : ControllerBase
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
