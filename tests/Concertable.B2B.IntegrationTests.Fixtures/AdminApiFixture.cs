using Concertable.B2B.Admin.Domain.Entities;
using Concertable.B2B.Admin.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

/// <summary>
/// Adds unfiltered, no-tracking read-back over the Admin module's tables — so tests assert the
/// persisted admin/invitation rows directly instead of resolving a context off the service provider
/// per test.
/// </summary>
public sealed class AdminApiFixture : ApiFixture
{
    private AdminDbContext adminDb = null!;
    private UserDbContext userDb = null!;

    public IQueryable<AdminInvitationEntity> AdminInvitations => adminDb.AdminInvitations.AsNoTracking();

    public Task<bool> IsAdminAsync(Guid sub) => adminDb.AdminProfiles.AnyAsync(p => p.Sub == sub);

    /// <summary>Simulates the first authenticated request after login (what every SPA calls right after
    /// signin) — reads back the real persisted email for <paramref name="userId"/> rather than accepting a
    /// caller-supplied copy that could silently drift from the row <c>RegisterAsync</c> actually wrote.</summary>
    public async Task<HttpResponseMessage> LogInAsync(Guid userId)
    {
        var user = await userDb.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        return await CreateClient(user).GetAsync("/api/auth/me");
    }

    /// <summary>Removes every seeded admin — profile and underlying user row alike — arranging the "no admin
    /// exists yet, and the bootstrap email is free to register" precondition the bootstrap path requires,
    /// which the standard seed graph never holds (it always seeds one admin at <c>SeedUsers.AdminEmail</c>).</summary>
    public async Task ClearAdminsAsync()
    {
        var subs = await adminDb.AdminProfiles.Select(p => p.Sub).ToListAsync();
        adminDb.AdminProfiles.RemoveRange(adminDb.AdminProfiles);
        await adminDb.SaveChangesAsync();

        userDb.Users.RemoveRange(userDb.Users.Where(u => subs.Contains(u.Id)));
        await userDb.SaveChangesAsync();
    }

    /// <summary>Inserts a pending admin invitation with a chosen <paramref name="expiresAt"/> — lets a test
    /// arrange an expired or otherwise time-specific invitation the invite endpoint (fixed 7-day TTL) can't
    /// produce. The email is stored normalized, matching the invite service + the registration-match lookup.</summary>
    public async Task<AdminInvitationEntity> AddAdminInvitationAsync(string email, Guid createdBy, DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        var invitation = AdminInvitationEntity.Create(email.Trim().ToLowerInvariant(), createdBy, now, expiresAt - now);
        invitation.ClearDomainEvents(); // an arranged invitation must not stage an invite email
        adminDb.AdminInvitations.Add(invitation);
        await adminDb.SaveChangesAsync();
        return invitation;
    }

    protected override void OnReset(IServiceScope scope)
    {
        adminDb = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        userDb = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    }
}
