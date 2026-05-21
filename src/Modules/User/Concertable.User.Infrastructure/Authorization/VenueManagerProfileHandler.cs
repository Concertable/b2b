using Concertable.User.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Concertable.User.Infrastructure.Authorization;

internal sealed class VenueManagerProfileRequirement : IAuthorizationRequirement { }

internal sealed class VenueManagerProfileHandler : AuthorizationHandler<VenueManagerProfileRequirement>
{
    private readonly UserDbContext db;

    public VenueManagerProfileHandler(UserDbContext db)
    {
        this.db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VenueManagerProfileRequirement requirement)
    {
        var subClaim = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var sub)) return;

        if (await db.VenueManagerProfiles.AnyAsync(p => p.Sub == sub))
            context.Succeed(requirement);
    }
}
