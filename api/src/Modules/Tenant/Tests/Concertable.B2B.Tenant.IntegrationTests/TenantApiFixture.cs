using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Concertable.B2B.Tenant.IntegrationTests;

public sealed class TenantApiFixture : ApiFixture
{
    private const long TenantCreationLockSeed = 638457220;
    private TenantDbContext dbContext = null!;
    private TenantProvisioningHandler provisioningHandler = null!;

    public IQueryable<TenantEntity> Tenants => dbContext.Tenants.AsNoTracking();
    public IQueryable<TenantMembershipEntity> Memberships => dbContext.Memberships.AsNoTracking();
    public IQueryable<TenantBusinessActivityEntity> BusinessActivities => dbContext.BusinessActivities.AsNoTracking();
    public IQueryable<TenantInvitationEntity> Invitations => dbContext.Invitations.AsNoTracking();
    public IQueryable<TenantVerificationEntity> Verifications =>
        dbContext.Verifications.Include(verification => verification.Documents).AsNoTracking();

    public Task ProvisionAsync(CredentialRegisteredEvent @event, MessageEnvelope? envelope = null) =>
        provisioningHandler.HandleAsync(
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

    public async Task ChangeMembershipRoleAsync(Guid tenantId, Guid userId, TenantRole role)
    {
        var membership = await dbContext.Memberships.SingleAsync(
            candidate => candidate.TenantId == tenantId && candidate.UserId == userId);
        membership.ChangeRole(role);
        await dbContext.SaveChangesAsync();
    }

    public async Task<TenantInvitationEntity> AddInvitationAsync(
        Guid tenantId,
        string email,
        TenantRole role,
        Guid inviterUserId,
        DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        var inviter = await dbContext.Memberships.SingleOrDefaultAsync(
            membership => membership.TenantId == tenantId && membership.UserId == inviterUserId);
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            email.Trim().ToLowerInvariant(),
            role,
            inviter?.Id ?? Guid.NewGuid(),
            inviter?.PermissionVersion ?? 1,
            now,
            expiresAt - now);
        invitation.ClearDomainEvents();
        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync();
        return invitation;
    }

    public async Task<TenantVerificationEntity> AddRejectedVerificationAsync(
        Guid tenantId,
        VerificationDocumentType documentType,
        string rejectionReason,
        DateTime rejectedAt)
    {
        var verification = TenantVerificationEntity.Submit(
            tenantId,
            [VerificationDocumentEntity.Create(documentType, $"seed-{Guid.NewGuid()}", rejectedAt)],
            rejectedAt);
        verification.Reject(Guid.NewGuid(), rejectionReason, rejectedAt);
        verification.ClearDomainEvents();
        dbContext.Verifications.Add(verification);
        await dbContext.SaveChangesAsync();
        return verification;
    }

    public async Task<TenantVerificationEntity> AddPendingVerificationAsync(
        Guid tenantId,
        VerificationDocumentType documentType,
        DateTime submittedAt)
    {
        var verification = TenantVerificationEntity.Submit(
            tenantId,
            [VerificationDocumentEntity.Create(documentType, $"seed-{Guid.NewGuid()}", submittedAt)],
            submittedAt);
        verification.ClearDomainEvents();
        dbContext.Verifications.Add(verification);
        await dbContext.SaveChangesAsync();
        return verification;
    }

    public async Task<T> RunWithTenantCreationBarrierAsync<T>(Guid userId, Func<Task<T>> action)
    {
        await using var control = new NpgsqlConnection(dbContext.Database.GetConnectionString());
        await control.OpenAsync();
        await ExecuteScalarAsync(
            control,
            "SELECT pg_advisory_lock(hashtextextended(CAST(@userId AS text), @seed))",
            userId);
        var lockHeld = true;
        var result = action();
        try
        {
            await WaitForTenantCreationWaitersAsync(control);
            await ExecuteScalarAsync(
                control,
                "SELECT pg_advisory_unlock(hashtextextended(CAST(@userId AS text), @seed))",
                userId);
            lockHeld = false;
            return await result;
        }
        finally
        {
            if (lockHeld)
                await ExecuteScalarAsync(
                    control,
                    "SELECT pg_advisory_unlock(hashtextextended(CAST(@userId AS text), @seed))",
                    userId);
        }
    }

    private static async Task WaitForTenantCreationWaitersAsync(NpgsqlConnection connection)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM pg_stat_activity
                WHERE datname = current_database()
                  AND pid <> pg_backend_pid()
                  AND wait_event = 'advisory'
                  AND query LIKE '%pg_advisory_xact_lock(hashtextextended%'
                """;
            if (Convert.ToInt32(await command.ExecuteScalarAsync(timeout.Token)) >= 2)
                return;
            await Task.Delay(25, timeout.Token);
        }
    }

    private static async Task ExecuteScalarAsync(NpgsqlConnection connection, string sql, Guid userId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("seed", TenantCreationLockSeed);
        await command.ExecuteScalarAsync();
    }

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        provisioningHandler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>()
            .OfType<TenantProvisioningHandler>()
            .Single();
    }
}
