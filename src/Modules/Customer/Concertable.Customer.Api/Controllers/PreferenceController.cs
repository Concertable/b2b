using Concertable.Customer.Application.DTOs;
using Concertable.Customer.Application.Interfaces;
using Concertable.Customer.Application.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.Customer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class PreferenceController : ControllerBase
{
    private readonly IPreferenceService preferenceService;

    public PreferenceController(IPreferenceService preferenceService)
    {
        this.preferenceService = preferenceService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePreferenceRequest request)
    {
        await preferenceService.CreateAsync(request);
        return Created();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PreferenceDto preferenceDto) =>
        Ok(await preferenceService.UpdateAsync(preferenceDto));

    [HttpGet("user")]
    public async Task<IActionResult> GetByUser() =>
        Ok(await preferenceService.GetByUserAsync());
}
