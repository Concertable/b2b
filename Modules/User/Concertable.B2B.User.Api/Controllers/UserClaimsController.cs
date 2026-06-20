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
    private readonly ILogger<UserClaimsController> logger;

    public UserClaimsController(IUserModule userModule, ILogger<UserClaimsController> logger)
    {
        this.userModule = userModule;
        this.logger = logger;
    }

    // Identity-only: B2B no longer mints `owner`. Acting authority is the request-scoped active tenant,
    // resolved per request from membership (X-Tenant-Id) — one claim can't represent a multi-tenant user,
    // and B2B's payout proxy now passes the tenant id to Payment explicitly (USER_MODEL_PLAN Phase 5).
    // `role` survives until Phase 7.
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

    public sealed record ClaimDto(string Type, string Value);
}
