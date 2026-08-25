using Concertable.B2B.Opportunity.Api.Mappers;
using Concertable.B2B.Opportunity.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Reunion.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Opportunity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class OpportunityController : ControllerBase
{
    private readonly IOpportunityService opportunityService;
    private readonly IOpportunityMapper mapper;

    public OpportunityController(
        IOpportunityService opportunityService,
        IOpportunityMapper mapper)
    {
        this.opportunityService = opportunityService;
        this.mapper = mapper;
    }

    [HttpGet("active/venue/{id}")]
    public async Task<ActionResult<IPagination<OpportunityResponse>>> GetActiveByVenueId(
        int id,
        [FromQuery] PageParams pageParams,
        CancellationToken ct)
    {
        var page = await opportunityService.GetActiveByVenueIdAsync(id, pageParams);
        return Ok(await mapper.ToResponsesAsync(page, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OpportunityResponse>> GetById(
        int id,
        CancellationToken ct)
    {
        var result = await opportunityService.GetByIdAsync(id);
        return (await result.MapAsync(
            opportunity => mapper.ToResponseAsync(opportunity, ct)))
            .ToOkOrProblem();
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost]
    public async Task<ActionResult<OpportunityResponse>> Create(
        [FromBody] OpportunityRequest request,
        CancellationToken ct) =>
        (await (await opportunityService.CreateAsync(request))
            .MapAsync(opportunity => mapper.ToResponseAsync(opportunity, ct)))
            .ToCreatedOrProblem(
                opportunity => $"/api/opportunity/{opportunity.Id}");

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost("bulk")]
    public async Task<ActionResult> CreateMultiple([FromBody] IEnumerable<OpportunityRequest> requests)
    {
        var result = await opportunityService.CreateMultipleAsync(requests);
        return result.ToActionResult(() => Created());
    }

    [HttpGet("/api/venue/{venueId:int}/opportunities")]
    public async Task<ActionResult<IReadOnlyList<OpportunityResponse>>> GetByVenueId(
        int venueId,
        CancellationToken ct)
    {
        var opportunities = await opportunityService.GetActiveByVenueIdAsync(venueId);
        return Ok(await mapper.ToResponsesAsync(opportunities, ct));
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPut("/api/venue/{venueId:int}/opportunities")]
    public async Task<ActionResult<IReadOnlyList<OpportunityResponse>>> Update(
        int venueId,
        [FromBody] IEnumerable<OpportunityRequest> desired,
        CancellationToken ct)
    {
        var result = await opportunityService.UpdateAsync(venueId, desired);
        return (await result.MapAsync(
            opportunities => mapper.ToResponsesAsync(opportunities, ct)))
            .ToOkOrProblem();
    }

    [HttpGet("{id}/ownership")]
    public async Task<ActionResult<bool>> IsOwner(int id)
    {
        return Ok(await opportunityService.OwnsOpportunityAsync(id));
    }

}
