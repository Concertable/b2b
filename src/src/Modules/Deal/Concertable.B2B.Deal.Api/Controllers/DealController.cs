using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.Kernel.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Deal.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class DealController : ControllerBase
{
    private readonly IDealService dealService;

    public DealController(IDealService dealService)
    {
        this.dealService = dealService;
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var deal = await dealService.GetByIdAsync(id)
            .OrNotFound($"Contract {id}");
        return Ok(deal);
    }
}
