using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.User.Api.Controllers;

[ApiController]
[Route("internal/users")]
[Authorize("UserClaimsScope")]
internal class UserClaimsController : ControllerBase
{
    private readonly IUserModule userModule;
    private readonly ILogger<UserClaimsController> logger;

    public UserClaimsController(IUserModule userModule, ILogger<UserClaimsController> logger)
    {
        this.userModule = userModule;
        this.logger = logger;
    }

    [HttpGet("{sub:guid}/claims")]
    public async Task<ActionResult<ClaimDto[]>> GetClaims(Guid sub)
    {
        var user = await userModule.GetByIdAsync(sub);
        if (user is null)
        {
            logger.UserClaimsUserNotFound(sub);
            return Ok(Array.Empty<ClaimDto>());
        }

        logger.UserClaimsReturned(sub, user.Role);
        return Ok(new[] { new ClaimDto("role", user.Role.ToString()) });
    }

    public record ClaimDto(string Type, string Value);
}
