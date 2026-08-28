using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Contracts;
using Reunion.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal sealed class OpportunityController : ControllerBase
{
    private readonly IOpportunityService opportunityService;
    private readonly IOpportunityDashboardService dashboardService;
    private readonly IOpportunityMapper mapper;

    public OpportunityController(
        IOpportunityService opportunityService,
        IOpportunityDashboardService dashboardService,
        IOpportunityMapper mapper)
    {
        this.opportunityService = opportunityService;
        this.dashboardService = dashboardService;
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
            .ToOkOrProblem(mapper.ToResponse);
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost]
    public async Task<ActionResult<OpportunityResponse>> Create([FromBody] OpportunityRequest request) =>
        (await opportunityService.CreateAsync(request))
            .ToCreatedOrProblem(
                mapper.ToResponse,
                opportunity => $"/api/opportunity/{opportunity.Id}");

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateMultiple([FromBody] IEnumerable<OpportunityRequest> requests)
    {
        var result = await opportunityService.CreateMultipleAsync(requests);
        return result.ToActionResult(() => Created());
    }

    [HttpGet("/api/venue/{venueId:int}/opportunities")]
    public async Task<IActionResult> GetByVenueId(int venueId)
    {
        var opportunities = await opportunityService.GetActiveByVenueIdAsync(venueId);
        return Ok(mapper.ToResponses(opportunities));
    }

    [HasPermission(VenuePermissions.OpportunitiesManage)]
    [HttpPut("/api/venue/{venueId:int}/opportunities")]
    public async Task<ActionResult<List<OpportunityResponse>>> Update(
        int venueId,
        [FromBody] IEnumerable<OpportunityRequest> desired)
    {
        var result = (await opportunityService.UpdateAsync(venueId, desired))
            .Map(opportunities => mapper.ToResponses(opportunities).ToList());
        return result.ToOkOrProblem();
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

    [HttpGet("venue/current")]
    [RequiredTenantType(TenantType.Venue)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<OpportunityApplicationMetricsResponse>>> GetOpenForCurrentVenue() =>
        (await dashboardService.GetApplicationMetricsForCurrentVenueAsync())
            .ToOkOrProblem(metrics => metrics.ToApplicationMetricsResponses());

    [HttpGet("artist/recommended")]
    [RequiredTenantType(TenantType.Artist)]
    [HasPermission(SharedPermissions.OperationsView)]
    public async Task<ActionResult<IReadOnlyList<OpportunityMatchResponse>>> GetRecommendedForCurrentArtist() =>
        (await dashboardService.GetMatchesForCurrentArtistAsync())
            .ToOkOrProblem(matches => matches.ToMatchResponses());
}
