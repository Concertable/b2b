using Concertable.B2B.Artist.Api.Mappers;
using Concertable.B2B.Artist.Api.Responses;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.Artist.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[RequiredTenantType(TenantType.Artist)]
internal sealed class ArtistController : ControllerBase
{
    private readonly IArtistService artistService;

    public ArtistController(IArtistService artistService)
    {
        this.artistService = artistService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsById(int id)
    {
        return (await artistService.GetDetailsByIdAsync(id))
            .Map(artist => artist.ToDetailsResponse())
            .ToOkOrProblem();
    }

    [HasPermission(SharedPermissions.OperationsView)]
    [HttpGet("user")]
    public async Task<ActionResult<DetailsResponse>> GetDetailsForCurrentUser() =>
        (await artistService.GetDetailsForCurrentUserAsync())
            .Map(artist => artist.ToDetailsResponse())
            .ToOkOrProblem();

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPost]
    public async Task<ActionResult<ArtistDetails>> Create([FromForm] CreateArtistRequest request)
    {
        return (await artistService.CreateAsync(request)).ToActionResult(
            artist => CreatedAtAction(nameof(GetDetailsById), new { Id = artist.Id }, artist));
    }

    [HasPermission(SharedPermissions.ProfileEdit)]
    [HttpPut("{id}")]
    public async Task<ActionResult<ArtistDetails>> Update(int id, [FromForm] UpdateArtistRequest request)
    {
        return (await artistService.UpdateAsync(id, request)).ToOkOrProblem();
    }
}
