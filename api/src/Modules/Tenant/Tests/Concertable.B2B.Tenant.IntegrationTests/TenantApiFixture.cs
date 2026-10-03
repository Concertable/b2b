using Concertable.B2B.DataAccess.Infrastructure;
using System.Security.Claims;
using System.Runtime.ExceptionServices;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Application.Errors;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Domain.Entities;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Reunion;
using RequestTenantResolver = Concertable.Kernel.Identity.ITenantResolver;

namespace Concertable.B2B.Tenant.IntegrationTests;

public sealed class TenantApiFixture : ApiFixture
{
    private const long TenantCreationLockSeed = 638457220;
    private TenantDbContext dbContext = null!;
    private ITransactionRunner transactionRunner = null!;
    private TenantProvisioningHandler provisioningHandler = null!;
    internal VerificationReviewRaceInterceptor VerificationReviewRace { get; } = new();

    public IQueryable<TenantEntity> Tenants => dbContext.Tenants.AsNoTracking();
    public IQueryable<TenantMembershipEntity> Memberships => dbContext.Memberships.Include(membership => membership.Assignments).AsNoTracking();
    public IQueryable<TenantBusinessActivityEntity> BusinessActivities => dbContext.BusinessActivities.AsNoTracking();
    public IQueryable<TenantInvitationEntity> Invitations => dbContext.Invitations.AsNoTracking();
    public IQueryable<TenantVerificationEntity> Verifications =>
        dbContext.Verifications.Include(verification => verification.Documents).AsNoTracking();

    public Task<TResult> ExecuteResolutionAsync<TResult>(
        Func<ITenantResolver, CancellationToken, Task<TResult>> resolve,
        CancellationToken ct = default) =>
        transactionRunner.ExecuteAsync(resolve, ct);

    internal async Task<UnitResult<RemoveMemberError>> RemoveOwnersInOneCommandAsync(
        Guid ownerUserId,
        Guid otherOwnerUserId,
        Guid tenantId)
    {
        var accessor = Services.GetRequiredService<IHttpContextAccessor>();
        var previous = accessor.HttpContext;
        var request = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", ownerUserId.ToString()),
                 new Claim(ClaimTypes.NameIdentifier, ownerUserId.ToString())],
                "Test"))
        };
        request.Request.Headers[TenantHeaders.TenantId] = tenantId.ToString();
        accessor.HttpContext = request;
        try
        {
            return await transactionRunner.ExecuteAsync<IServiceProvider, UnitResult<RemoveMemberError>>(
                async (services, ct) =>
                {
                    await services.GetRequiredService<RequestTenantResolver>().ResolveAsync(ct);
                    var members = services.GetRequiredService<IMembershipService>();
                    var first = await members.RemoveMemberAsync(otherOwnerUserId, ct);
                    if (first.TryGetError(out var firstError))
                        throw new InvalidOperationException($"First owner removal failed: {firstError}.");
                    var second = await members.RemoveMemberAsync(ownerUserId, ct);
                    if (second.TryGetError(out var secondError))
                        throw new InvalidOperationException($"Second owner removal failed: {secondError}.");
                    return second;
                });
        }
        finally
        {
            accessor.HttpContext = previous;
        }
    }

    public Task ProvisionAsync(CredentialRegisteredEvent @event, MessageEnvelope? envelope = null) =>
        provisioningHandler.HandleAsync(
            @event,
            envelope ?? MessageEnvelope.Create<CredentialRegisteredEvent>(DateTimeOffset.UtcNow));

    public Task AddOwnerMembershipAsync(Guid tenantId, Guid userId) =>
        AddMembershipAsync(tenantId, userId, "Owner");

    public async Task AddMembershipAsync(Guid tenantId, Guid userId, string presetKey)
    {
        dbContext.Memberships.Add(
            TenantMembershipEntity.Create(tenantId, userId, [RoleId(tenantId, presetKey)], invitedBy: null, DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
    }

    public async Task ChangeMembershipRolesAsync(Guid tenantId, Guid userId, params string[] presetKeys)
    {
        var membership = await dbContext.Memberships.SingleAsync(
            candidate => candidate.TenantId == tenantId && candidate.UserId == userId);
        membership.ReplaceRoles([.. presetKeys.Select(key => RoleId(tenantId, key))], Guid.NewGuid(), DateTime.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    public async Task<TenantInvitationEntity> AddInvitationAsync(
        Guid tenantId,
        string email,
        string presetKey,
        Guid inviterUserId,
        DateTime expiresAt)
    {
        var now = DateTime.UtcNow;
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(value => value.Id == tenantId);
        var inviter = await dbContext.Memberships.SingleOrDefaultAsync(
            membership => membership.TenantId == tenantId && membership.UserId == inviterUserId);
        var invitation = TenantInvitationEntity.Create(
            tenantId,
            email.Trim().ToLowerInvariant(),
            [RoleId(tenantId, presetKey)],
            inviter?.Id ?? Guid.NewGuid(),
            inviter?.PermissionVersion ?? 1,
            tenant?.RolePolicyVersion ?? 1,
            now,
            expiresAt - now);
        invitation.ClearDomainEvents();
        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync();
        return invitation;
    }
    internal static Guid RoleId(Guid tenantId, string presetKey) => SystemPresetIds.For(tenantId, presetKey);

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

    public async Task<T> RunWithTenantCreationBarrierAsync<T>(
        Guid userId,
        Func<Task<T>> action,
        Action? afterFirstWaiter = null)
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
            if (afterFirstWaiter is not null)
            {
                await WaitForTenantCreationWaitersAsync(control, 1);
                afterFirstWaiter();
            }
            await WaitForTenantCreationWaitersAsync(control, 2);
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

    public async Task<(TDeletion Deletion, TContender Contender)> RunWithPausedTenantDeletionAsync<TDeletion, TContender>(
        Guid invitationId,
        Func<Task<TDeletion>> deletion,
        Func<Task<TContender>> contender)
    {
        var connectionString = dbContext.Database.GetConnectionString();
        await using var monitor = new NpgsqlConnection(connectionString);
        await monitor.OpenAsync();
        await using var control = new NpgsqlConnection(connectionString);
        await control.OpenAsync();
        await using var transaction = await control.BeginTransactionAsync();
        await using (var command = control.CreateCommand())
        {
            command.CommandText = """
                SELECT 1
                FROM tenant."Invitations"
                WHERE "Id" = @invitationId
                FOR SHARE
                """;
            command.Parameters.AddWithValue("invitationId", invitationId);
            await command.ExecuteScalarAsync();
        }

        Task<TDeletion> deletionResult;
        Task<TContender> contenderResult;
        try
        {
            deletionResult = deletion();
            await WaitForLockWaiterAsync(monitor, """%DELETE FROM tenant."Invitations"%""");
            contenderResult = contender();
            await WaitForLockWaiterAsync(monitor, """%tenant."Tenants"%FOR UPDATE%""");
        }
        finally
        {
            await transaction.RollbackAsync();
        }
        return (await deletionResult, await contenderResult);
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

    private static async Task WaitForTenantCreationWaitersAsync(NpgsqlConnection connection, int count)
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
            if (Convert.ToInt32(await command.ExecuteScalarAsync(timeout.Token)) >= count)
                return;
            await Task.Delay(25, timeout.Token);
        }
    }

    private static async Task WaitForLockWaiterAsync(NpgsqlConnection connection, string queryPattern)
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
                  AND wait_event_type = 'Lock'
                  AND query LIKE @queryPattern
                """;
            command.Parameters.AddWithValue("queryPattern", queryPattern);
            if (Convert.ToInt32(await command.ExecuteScalarAsync(timeout.Token)) > 0)
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
        transactionRunner = scope.ServiceProvider.GetRequiredService<ITransactionRunner>();
        VerificationReviewRace.UseDataSource(scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>());
        provisioningHandler = scope.ServiceProvider
            .GetServices<IIntegrationEventHandler<CredentialRegisteredEvent>>()
            .OfType<TenantProvisioningHandler>()
            .Single();
    }
}
