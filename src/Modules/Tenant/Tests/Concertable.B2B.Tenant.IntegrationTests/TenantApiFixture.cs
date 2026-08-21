using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Tenant.IntegrationTests;

public sealed class TenantApiFixture : ApiFixture
{
    private TenantDbContext dbContext = null!;
    private TenantProvisioningHandler provisioning = null!;

    public IQueryable<TenantEntity> Tenants => dbContext.Tenants.AsNoTracking();
    public IQueryable<TenantMembershipEntity> Memberships => dbContext.Memberships.AsNoTracking();
    public IQueryable<TenantInvitationEntity> Invitations => dbContext.Invitations.AsNoTracking();

    public Task ProvisionAsync(CredentialRegisteredEvent @event, MessageEnvelope? envelope = null) =>
        provisioning.HandleAsync(
            @event,
            envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));

    public Task AddOwnerMembershipAsync(Guid tenantId, Guid userId) =>
        AddMembershipAsync(tenantId, userId, TenantRole.Owner);

    public async Task AddMembershipAsync(Guid tenantId, Guid userId, TenantRole role)
    {
        dbContext.Memberships.Add(
            TenantMembershipEntity.Create(tenantId, userId, role, invitedBy: null, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    public async Task<TenantInvitationEntity> AddInvitationAsync(
        Guid tenantId,
        string email,
        TenantRole role,
        Guid createdBy,
        DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        var tenant = await dbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(value => value.Id == tenantId);
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            tenant?.Type ?? TenantType.Venue,
            email.Trim().ToLowerInvariant(),
            role,
            createdBy,
            now,
            expiresAt - now);
        invitation.ClearDomainEvents();
        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync();
        return invitation;
    }

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        provisioning = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>()
            .OfType<TenantProvisioningHandler>()
            .Single();
    }
}
