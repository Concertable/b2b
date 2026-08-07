using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Contracts;
using Concertable.Shared.Api.Results;
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
    public async Task<ActionResult<IDeal>> GetById(int id) =>
        (await dealService.GetByIdAsync(id)).ToActionResult(value => new ActionResult<IDeal>(value));
}
