using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.User.Api.Controllers;

[ApiController]
[Route("internal/users")]
[Authorize("UserClaimsScope")]
internal sealed class UserClaimsController : ControllerBase
{
    private readonly IUserModule userModule;
    private readonly ITenantModule tenantModule;
    private readonly ILogger<UserClaimsController> logger;

    public UserClaimsController(IUserModule userModule, ITenantModule tenantModule, ILogger<UserClaimsController> logger)
    {
        this.userModule = userModule;
        this.tenantModule = tenantModule;
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

        var claims = new List<ClaimDto> { new("role", user.Role.ToString()) };
        if (await tenantModule.GetTenantIdByUserIdAsync(sub) is { } tenantId)
            claims.Add(new ClaimDto("owner", tenantId.ToString()));

        logger.UserClaimsReturned(sub, user.Role);
        return Ok(claims.ToArray());
    }

    public sealed record ClaimDto(string Type, string Value);
}
