using Concertable.B2B.Admin.Application.Interfaces;
using Concertable.B2B.Admin.Domain.Entities;
using Concertable.B2B.Admin.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Admin.IntegrationTests;

public sealed class AdminApiFixture : ApiFixture
{
    private AdminDbContext adminDb = null!;
    private IAdminService adminService = null!;

    public IQueryable<AdminInvitationEntity> AdminInvitations =>
        adminDb.AdminInvitations.AsNoTracking();

    public Task<bool> IsAdminAsync(Guid sub) =>
        adminDb.AdminProfiles.AnyAsync(profile => profile.Sub == sub);

    public Task GrantIfEligibleAsync(Guid sub, string email) =>
        adminService.GrantIfEligibleAsync(sub, email);

    public async Task ClearAdminsAsync()
    {
        adminDb.AdminProfiles.RemoveRange(adminDb.AdminProfiles);
        await adminDb.SaveChangesAsync();
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
        adminDb.AdminInvitations.Add(invitation);
        await adminDb.SaveChangesAsync();
        return invitation;
    }

    protected override void OnReset(IServiceScope scope)
    {
        adminDb = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
        adminService = scope.ServiceProvider.GetRequiredService<IAdminService>();
    }
}
