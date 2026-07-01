using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[TenantPersona(TenantType.Venue)]
internal sealed class OpportunityController : ControllerBase
{
    private readonly IOpportunityService opportunityService;
    private readonly IOpportunityResponseMapper mapper;

    public OpportunityController(IOpportunityService opportunityService, IOpportunityResponseMapper mapper)
    {
        this.opportunityService = opportunityService;
        this.mapper = mapper;
    }

    [HttpGet("active/venue/{id}")]
    public async Task<IActionResult> GetActiveByVenueId(int id, [FromQuery] PageParams pageParams)
    {
        var page = await opportunityService.GetActiveByVenueIdAsync(id, pageParams);
        return Ok(mapper.ToResponses(page));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityResponse>> GetById(int id)
    {
        var opportunity = await opportunityService.GetByIdAsync(id);
        return Ok(mapper.ToResponse(opportunity));
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] OpportunityRequest request)
    {
        var opportunity = await opportunityService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = opportunity.Id }, mapper.ToResponse(opportunity));
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateMultiple([FromBody] IEnumerable<OpportunityRequest> requests)
    {
        await opportunityService.CreateMultipleAsync(requests);
        return Created();
    }

    [HttpGet("/api/Venue/{venueId:int}/opportunities")]
    public async Task<IActionResult> GetByVenueId(int venueId)
    {
        var opportunities = await opportunityService.GetActiveByVenueIdAsync(venueId);
        return Ok(mapper.ToResponses(opportunities));
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPut("/api/Venue/{venueId:int}/opportunities")]
    public async Task<IActionResult> Update(int venueId, [FromBody] IEnumerable<OpportunityRequest> desired)
    {
        var updated = await opportunityService.UpdateAsync(venueId, desired);
        return Ok(mapper.ToResponses(updated));
    }

    [HttpGet("{id}/ownership")]
    public async Task<IActionResult> IsOwner(int id)
    {
        return Ok(await opportunityService.OwnsOpportunityAsync(id));
    }

    [HttpGet("by-application/{applicationId}/ownership")]
    public async Task<IActionResult> IsOwnerByApplicationId(int applicationId)
    {
        return Ok(await opportunityService.OwnsOpportunityByApplicationIdAsync(applicationId));
    }
}
