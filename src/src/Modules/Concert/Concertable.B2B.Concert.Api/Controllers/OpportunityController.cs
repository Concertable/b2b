using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Errors;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Concertable.Shared.Api.Results;
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
        return (await opportunityService.GetByIdAsync(id))
            .Map(mapper.ToResponse)
            .OrFailure(() => ConcertLookupError.OpportunityNotFound(id))
            .ToOkActionResult();
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost]
    public async Task<ActionResult<OpportunityResponse>> Create([FromBody] OpportunityRequest request)
    {
        var result = (await opportunityService.CreateAsync(request))
            .Map(mapper.ToResponse);
        return result.ToActionResult(
            opportunity => CreatedAtAction(nameof(GetById), new { id = opportunity.Id }, opportunity));
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateMultiple([FromBody] IEnumerable<OpportunityRequest> requests)
    {
        var result = await opportunityService.CreateMultipleAsync(requests);
        return result.ToActionResult(() => Created());
    }

    [HttpGet("/api/Venue/{venueId:int}/opportunities")]
    public async Task<IActionResult> GetByVenueId(int venueId)
    {
        var opportunities = await opportunityService.GetActiveByVenueIdAsync(venueId);
        return Ok(mapper.ToResponses(opportunities));
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPut("/api/Venue/{venueId:int}/opportunities")]
    public async Task<ActionResult<List<OpportunityResponse>>> Update(
        int venueId,
        [FromBody] IEnumerable<OpportunityRequest> desired)
    {
        var result = (await opportunityService.UpdateAsync(venueId, desired))
            .Map(opportunities => mapper.ToResponses(opportunities).ToList());
        return result.ToOkActionResult();
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
