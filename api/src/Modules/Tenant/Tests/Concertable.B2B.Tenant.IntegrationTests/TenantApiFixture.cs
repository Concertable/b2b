using System.Runtime.ExceptionServices;
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
    internal VerificationReviewRaceInterceptor VerificationReviewRace { get; } = new();

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

    public Task<T> RunWithVerificationReviewBarrierAsync<T>(
        Guid tenantId,
        Func<CancellationToken, Task<T>> action) =>
        RunWithVerificationReviewBarrierCoreAsync(tenantId, action, null);

    public Task<T> RunWithVerificationReviewBarrierAsync<T>(
        Guid tenantId,
        Func<CancellationToken, Task<T>> action,
        Exception injectedFailure) =>
        RunWithVerificationReviewBarrierCoreAsync(tenantId, action, injectedFailure);

    private async Task<T> RunWithVerificationReviewBarrierCoreAsync<T>(
        Guid tenantId,
        Func<CancellationToken, Task<T>> action,
        Exception? injectedFailure)
    {
        using var cancellation = new CancellationTokenSource();
        await using var control = new NpgsqlConnection(dbContext.Database.GetConnectionString());
        await control.OpenAsync();
        await using var transaction = await control.BeginTransactionAsync();
        Task<T>? result = null;
        ExceptionDispatchInfo? failure = null;
        T? outcome = default;
        var completed = false;
        var transactionCompleted = false;

        try
        {
            await using (var command = control.CreateCommand())
            {
                command.CommandText = """
                    SELECT 1
                    FROM tenant."Verifications"
                    WHERE "TenantId" = @tenantId
                    FOR UPDATE
                    """;
                command.Parameters.AddWithValue("tenantId", tenantId);
                await command.ExecuteScalarAsync();
            }

            VerificationReviewRace.Arm();
            result = action(cancellation.Token);
            await VerificationReviewRace.WaitForCompetingLockWaitsAsync();
            if (injectedFailure is not null)
                throw injectedFailure;
            await transaction.CommitAsync();
            transactionCompleted = true;
            outcome = await result.WaitAsync(TimeSpan.FromSeconds(10));
            completed = true;
        }
        catch (Exception exception)
        {
            failure = ExceptionDispatchInfo.Capture(exception);
        }

        if (!transactionCompleted)
        {
            try
            {
                await transaction.RollbackAsync();
                transactionCompleted = true;
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture(exception);
            }
        }

        if (result is not null && !completed)
        {
            try
            {
                await cancellation.CancelAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture(exception);
            }

            try
            {
                outcome = await result.WaitAsync(TimeSpan.FromSeconds(10));
                completed = true;
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture(exception);
            }
        }

        failure?.Throw();
        if (!completed)
            throw new InvalidOperationException("The verification review barrier action did not complete.");
        return outcome!;
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

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.AddResettables(VerificationReviewRace);
        services.ConfigureDbContext<TenantDbContext>(
            (_, options) => options.AddInterceptors(VerificationReviewRace));
    }

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        VerificationReviewRace.UseDataSource(scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>());
        provisioningHandler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>()
            .OfType<TenantProvisioningHandler>()
            .Single();
    }
}
