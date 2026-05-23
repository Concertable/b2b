using Concertable.B2B.User.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Authorization;

internal sealed class ArtistManagerProfileRequirement : IAuthorizationRequirement { }

internal sealed class ArtistManagerProfileHandler : AuthorizationHandler<ArtistManagerProfileRequirement>
{
    private readonly UserDbContext db;

    public ArtistManagerProfileHandler(UserDbContext db)
    {
        this.db = db;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ArtistManagerProfileRequirement requirement)
    {
        var subClaim = context.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var sub)) return;

        if (await db.ArtistManagerProfiles.AnyAsync(p => p.Sub == sub))
            context.Succeed(requirement);
    }
}
