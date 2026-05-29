using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Concertable.B2B.User.Api.Controllers;

[ApiController]
[Route("internal/users")]
[Authorize("UserClaimsScope")]
internal class UserClaimsController : ControllerBase
{
    private readonly IUserModule userModule;

    public UserClaimsController(IUserModule userModule)
    {
        this.userModule = userModule;
    }

    [HttpGet("{sub:guid}/claims")]
    public async Task<ActionResult<ClaimDto[]>> GetClaims(Guid sub)
    {
        var user = await userModule.GetByIdAsync(sub);
        if (user is null)
            return Ok(Array.Empty<ClaimDto>());

        return Ok(new[] { new ClaimDto("role", user.Role.ToString()) });
    }

    public record ClaimDto(string Type, string Value);
}
