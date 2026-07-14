using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.Kernel.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Deal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class DealController : ControllerBase
{
    private readonly IDealService contractService;

    public DealController(IDealService contractService)
    {
        this.contractService = contractService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await contractService.GetByIdAsync(id)
            .OrNotFound($"Contract {id}");
        return Ok(contract);
    }
}
