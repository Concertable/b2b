using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Concert.Api.Mappers;
using Concertable.B2B.Concert.Api.Requests;
using Concertable.B2B.Concert.Api.Responses;
using Concertable.B2B.User.Api.Authorization;
using Concertable.B2B.User.Contracts;
using Microsoft.AspNetCore.Mvc;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Api.Controllers;

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

    [VenueManager]
    [HttpGet("opportunity/{id}")]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetAllByOpportunityId(int id)
    {
        var applications = await applicationService.GetByOpportunityIdAsync(id);
        return Ok(mapper.ToResponses(applications));
    }

    [ArtistManager]
    [HttpPost("{opportunityId}")]
    public async Task<IActionResult> Apply(int opportunityId, [FromBody] ApplyRequest? request = null)
    {
        var application = request is not null
            ? await applicationService.ApplyAsync(opportunityId, request.PaymentMethodId)
            : await applicationService.ApplyAsync(opportunityId);
        return CreatedAtAction(nameof(GetById), new { id = application.Id }, mapper.ToResponse(application));
    }

    [HttpGet("artist/pending")]
    [ArtistManager]
    public async Task<ActionResult<IEnumerable<ApplicationResponse>>> GetPendingForArtist()
    {
        var applications = await applicationService.GetPendingForArtistAsync();
        return Ok(mapper.ToResponses(applications));
    }

    [HttpGet("artist/recently-denied")]
    [ArtistManager]
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

    [ArtistManager]
    [HttpGet("opportunity/{opportunityId}/eligibility")]
    public async Task<ActionResult<bool>> CanApply(int opportunityId)
    {
        var artistId = await artistModule.GetIdByUserIdAsync(currentUser.GetId());
        if (artistId is null) return Ok(false);

        var result = await applicationValidator.CanApplyAsync(opportunityId, artistId.Value);
        return Ok(result.IsSuccess);
    }

    [VenueManager]
    [HttpGet("{applicationId}/eligibility")]
    public async Task<ActionResult<bool>> CanAccept(int applicationId)
    {
        var result = await applicationValidator.CanAcceptAsync(applicationId);
        return Ok(result.IsSuccess);
    }

    [ArtistManager]
    [HttpPost("opportunity/{opportunityId}/checkout")]
    public async Task<IActionResult> ApplyCheckout(int opportunityId)
    {
        var checkout = await applicationService.ApplyCheckoutAsync(opportunityId);
        return Ok(checkout);
    }

    [VenueManager]
    [HttpPost("{applicationId}/checkout")]
    public async Task<IActionResult> AcceptCheckout(int applicationId)
    {
        var checkout = await applicationService.AcceptCheckoutAsync(applicationId);
        return Ok(checkout);
    }

    [VenueManager]
    [HttpPost("{applicationId}/accept")]
    public async Task<IActionResult> Accept(int applicationId, [FromBody] AcceptRequest? request = null)
    {
        await applicationService.AcceptAsync(applicationId, request?.PaymentMethodId);
        return NoContent();
    }

}
