using Concertable.User.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Concertable.User.Infrastructure.Authorization;

internal sealed class AdminProfileRequirement : IAuthorizationRequirement { }

internal sealed class AdminProfileHandler : AuthorizationHandler<AdminProfileRequirement>
{
    private readonly UserDbContext db;

    public AdminProfileHandler(UserDbContext db)
    {
        this.db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminProfileRequirement requirement)
    {
        var subClaim = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var sub)) return;

        if (await db.AdminProfiles.AnyAsync(p => p.Sub == sub))
            context.Succeed(requirement);
    }
}
