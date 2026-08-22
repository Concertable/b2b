using Concertable.B2B.Admin.Domain.Entities;
using Concertable.B2B.Admin.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Admin.IntegrationTests;

public sealed class AdminApiFixture : ApiFixture
{
    private AdminDbContext dbContext = null!;

    public IQueryable<AdminInvitationEntity> AdminInvitations =>
        dbContext.AdminInvitations.AsNoTracking();

    public Task<bool> IsAdminAsync(Guid sub) =>
        dbContext.AdminProfiles.AnyAsync(profile => profile.Sub == sub);

    public async Task ClearAdminsAsync()
    {
        dbContext.AdminProfiles.RemoveRange(dbContext.AdminProfiles);
        await dbContext.SaveChangesAsync();
    }

    public async Task<AdminInvitationEntity> AddAdminInvitationAsync(
        string email,
        Guid createdBy,
        DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        var invitation = AdminInvitationEntity.Create(
            email.Trim().ToLowerInvariant(),
            createdBy,
            now,
            expiresAt - now);
        invitation.ClearDomainEvents();
        dbContext.AdminInvitations.Add(invitation);
        await dbContext.SaveChangesAsync();
        return invitation;
    }

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    }
}
