using Concertable.Artist.Contracts;
using Concertable.Concert.Api.Mappers;
using Concertable.Concert.Api.Requests;
using Concertable.Concert.Api.Responses;
using Concertable.User.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Concertable.Kernel.Identity;

namespace Concertable.Concert.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
internal class ApplicationController : ControllerBase
{
    private readonly IApplicationService applicationService;
    private readonly IApplicationValidator applicationValidator;
    private readonly IArtistModule artistModule;
    private readonly ICurrentUser currentUser;
    private readonly IApplicationResponseMapper mapper;

    public ApplicationController(
        IApplicationService applicationService,
        IApplicationValidator applicationValidator,
        IArtistModule artistModule,
        ICurrentUser currentUser,
        IApplicationResponseMapper mapper)
    {
        this.applicationService = applicationService;
        this.applicationValidator = applicationValidator;
        this.artistModule = artistModule;
        this.currentUser = currentUser;
        this.mapper = mapper;
    }

    [Authorize(Policy = "VenueManager")]
    [HttpGet("opportunity/{id}")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetAllByOpportunityId(int id)
    {
        var applications = await applicationService.GetByOpportunityIdAsync(id);
        return Ok(mapper.ToResponses(applications));
    }

    [Authorize(Policy = "ArtistManager")]
    [HttpPost("{opportunityId}")]
    public async Task<IActionResult> Apply(int opportunityId, [FromBody] ApplyRequest? request = null)
    {
        var application = request is not null
            ? await applicationService.ApplyAsync(opportunityId, request.PaymentMethodId)
            : await applicationService.ApplyAsync(opportunityId);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, mapper.ToResponse(application));
    }

    [HttpGet("artist/pending")]
    [Authorize(Policy = "ArtistManager")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetPendingForArtist()
    {
        var applications = await applicationService.GetPendingForArtistAsync();
        return Ok(mapper.ToResponses(applications));
    }

    [HttpGet("artist/recently-denied")]
    [Authorize(Policy = "ArtistManager")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetRecentDeniedForArtist()
    {
        var applications = await applicationService.GetRecentDeniedForArtistAsync();
        return Ok(mapper.ToResponses(applications));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        var application = await applicationService.GetByIdAsync(id);
        return Ok(mapper.ToResponse(application));
    }

    [Authorize(Policy = "ArtistManager")]
    [HttpGet("opportunity/{opportunityId}/eligibility")]
    public async Task<ActionResult<bool>> CanApply(int opportunityId)
    {
        var artistId = await artistModule.GetIdByUserIdAsync(currentUser.GetId());
        if (artistId is null) return Ok(false);

        var result = await applicationValidator.CanApplyAsync(opportunityId, artistId.Value);
        return Ok(result.IsSuccess);
    }

    [Authorize(Policy = "VenueManager")]
    [HttpGet("{applicationId}/eligibility")]
    public async Task<ActionResult<bool>> CanAccept(int applicationId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);
        return Ok(result.IsSuccess);
    }

    [Authorize(Policy = "ArtistManager")]
    [HttpPost("opportunity/{opportunityId}/checkout")]
    public async Task<IActionResult> ApplyCheckout(int opportunityId)
    {
        var checkout = await applicationService.ApplyCheckoutAsync(opportunityId);
        return Ok(checkout);
    }

    [Authorize(Policy = "VenueManager")]
    [HttpPost("{applicationId}/checkout")]
    public async Task<IActionResult> AcceptCheckout(int applicationId)
    {
        var checkout = await applicationService.AcceptCheckoutAsync(applicationId);
        return Ok(checkout);
    }

    [Authorize(Policy = "VenueManager")]
    [HttpPost("{applicationId}/accept")]
    public async Task<IActionResult> Accept(int applicationId, [FromBody] AcceptRequest? request = null)
    {
        await applicationService.AcceptAsync(applicationId, request?.PaymentMethodId);
        return NoContent();
    }

}
