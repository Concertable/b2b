using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

/// <summary>
/// Adds unfiltered, no-tracking read-back over the Tenant module's tables — so tests assert the persisted
/// membership/tenant rows directly instead of resolving a context off the service provider per test.
/// </summary>
public sealed class TenantApiFixture : ApiFixture
{
    private TenantDbContext tenantDb = null!;

    public IQueryable<TenantEntity> Tenants => tenantDb.Tenants.AsNoTracking();
    public IQueryable<TenantMembershipEntity> Memberships => tenantDb.Memberships.AsNoTracking();
    public IQueryable<TenantInvitationEntity> Invitations => tenantDb.Invitations.AsNoTracking();
    public IQueryable<TenantVerificationEntity> Verifications =>
        tenantDb.Verifications.Include(v => v.Documents).AsNoTracking();

    /// <summary>Grants <paramref name="userId"/> an Owner membership in <paramref name="tenantId"/> — lets a test
    /// arrange the multi-membership case the seed graph never holds (every seeded operator owns one tenant).</summary>
    public Task AddOwnerMembershipAsync(Guid tenantId, Guid userId) =>
        AddMembershipAsync(tenantId, userId, TenantRole.Owner);

    /// <summary>Grants <paramref name="userId"/> a membership of <paramref name="role"/> in <paramref name="tenantId"/>
    /// — lets a test arrange any active role (e.g. a non-payout role to assert the permission gate).</summary>
    public async Task AddMembershipAsync(Guid tenantId, Guid userId, TenantRole role)
    {
        tenantDb.Memberships.Add(
            TenantMembershipEntity.Create(tenantId, userId, role, invitedBy: null, DateTime.UtcNow));
        await tenantDb.SaveChangesAsync();
    }

    /// <summary>Inserts a pending invitation with a chosen <paramref name="expiresAt"/> — lets a test arrange an
    /// expired or otherwise time-specific invitation the invite endpoint (fixed 7-day TTL) can't produce. The
    /// email is stored normalized, matching the invite service + the registration-match lookup.</summary>
    public async Task<TenantInvitationEntity> AddInvitationAsync(Guid tenantId, string email, TenantRole role, Guid createdBy, DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        var tenant = await tenantDb.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);
        var invitation = TenantInvitationEntity.Create(
            tenantId, tenant?.Type ?? TenantType.Venue, email.Trim().ToLowerInvariant(), role, createdBy, now, expiresAt - now);
        invitation.ClearDomainEvents(); // an arranged invitation must not stage an invite email
        tenantDb.Invitations.Add(invitation);
        await tenantDb.SaveChangesAsync();
        return invitation;
    }

    /// <summary>Inserts a rejected verification directly — arranges the resubmission case without depending on
    /// the admin review endpoints (a later phase). Documents carry an arbitrary seeded blob name; no real blob
    /// is written.</summary>
    public async Task<TenantVerificationEntity> AddRejectedVerificationAsync(
        Guid tenantId, VerificationDocumentType documentType, string rejectionReason, DateTime rejectedAt)
    {
        var verification = TenantVerificationEntity.Submit(
            tenantId,
            [VerificationDocumentEntity.Create(documentType, $"seed-{Guid.NewGuid()}", rejectedAt)],
            rejectedAt);
        verification.Reject(Guid.NewGuid(), rejectionReason, rejectedAt);
        verification.ClearDomainEvents();
        tenantDb.Verifications.Add(verification);
        await tenantDb.SaveChangesAsync();
        return verification;
    }

    protected override void OnReset(IServiceScope scope)
    {
        tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
    }
}
