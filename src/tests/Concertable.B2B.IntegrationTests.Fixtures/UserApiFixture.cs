using Concertable.B2B.User.Domain.Entities;
using Concertable.B2B.User.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

/// <summary>
/// Adds unfiltered, no-tracking read-back over the User module's admin tables — so tests assert the
/// persisted admin/invitation rows directly instead of resolving a context off the service provider
/// per test.
/// </summary>
public sealed class UserApiFixture : ApiFixture
{
    private UserDbContext userDb = null!;

    public IQueryable<AdminInvitationEntity> AdminInvitations => userDb.AdminInvitations.AsNoTracking();

    public Task<bool> IsAdminAsync(Guid sub) => userDb.AdminProfiles.AnyAsync(p => p.Sub == sub);

    /// <summary>Removes every seeded admin — profile and underlying user row alike — arranging the "no admin
    /// exists yet, and the bootstrap email is free to register" precondition the bootstrap path requires,
    /// which the standard seed graph never holds (it always seeds one admin at <c>SeedUsers.AdminEmail</c>).</summary>
    public async Task ClearAdminsAsync()
    {
        var subs = await userDb.AdminProfiles.Select(p => p.Sub).ToListAsync();
        userDb.AdminProfiles.RemoveRange(userDb.AdminProfiles);
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
        userDb.AdminInvitations.Add(invitation);
        await userDb.SaveChangesAsync();
        return invitation;
    }

    protected override void OnReset(IServiceScope scope)
    {
        userDb = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    }
}
