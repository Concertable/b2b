using System.Runtime.ExceptionServices;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Concertable.B2B.Conversations.IntegrationTests;

public sealed class ConversationsApiFixture : ApiFixture
{
    private const long ConversationCreationLockSeed = 638457221;
    private string connectionString = null!;

    internal async Task<T[]> RunWithConversationCreationBarrierAsync<T>(
        Guid creatorTenantId,
        Guid createdByMembershipId,
        Guid requestId,
        Func<CancellationToken, Task<T>[]> action)
    {
        using var cancellation = new CancellationTokenSource();
        await using var control = new NpgsqlConnection(connectionString);
        Task<T[]>? result = null;
        ExceptionDispatchInfo? failure = null;
        T[]? outcome = null;
        var completed = false;
        var lockHeld = false;
        var cancellationRequested = false;

        try
        {
            await control.OpenAsync();
            await SetConversationCreationLockAsync(
                control,
                creatorTenantId,
                createdByMembershipId,
                requestId,
                acquire: true);
            lockHeld = true;
            var actions = action(cancellation.Token);
            result = Task.WhenAll(actions);
            var firstAction = Task.WhenAny(actions);
            var waiters = WaitForConversationCreationLockWaitersAsync();
            if (await Task.WhenAny(firstAction, waiters) == firstAction)
                await await firstAction;
            await waiters;
        }
        catch (Exception exception)
        {
            failure = ExceptionDispatchInfo.Capture(exception);
        }

        if (lockHeld)
        {
            try
            {
                await SetConversationCreationLockAsync(
                    control,
                    creatorTenantId,
                    createdByMembershipId,
                    requestId,
                    acquire: false);
                lockHeld = false;
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture(exception);
                try
                {
                    await control.CloseAsync();
                }
                catch (Exception closeException)
                {
                    failure ??= ExceptionDispatchInfo.Capture(closeException);
                }
            }
        }

        if (result is not null && !completed)
        {
            if (failure is not null)
            {
                try
                {
                    await cancellation.CancelAsync().WaitAsync(TimeSpan.FromSeconds(5));
                    cancellationRequested = true;
                }
                catch (Exception cancellationException)
                {
                    failure ??= ExceptionDispatchInfo.Capture(cancellationException);
                }
            }

            try
            {
                outcome = await result.WaitAsync(TimeSpan.FromSeconds(10));
                completed = true;
            }
            catch (Exception exception)
            {
                failure ??= ExceptionDispatchInfo.Capture(exception);
                if (!cancellationRequested)
                {
                    try
                    {
                        await cancellation.CancelAsync().WaitAsync(TimeSpan.FromSeconds(5));
                    }
                    catch (Exception cancellationException)
                    {
                        failure ??= ExceptionDispatchInfo.Capture(cancellationException);
                    }
                }
            }
        }

        failure?.Throw();
        if (!completed)
            throw new InvalidOperationException("The conversation creation barrier action did not complete.");
        return outcome!;
    }

    private async Task WaitForConversationCreationLockWaitersAsync()
    {
        await using var observer = new NpgsqlConnection(connectionString);
        await observer.OpenAsync();
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        do
        {
            await using var command = observer.CreateCommand();
            command.CommandText = """
                SELECT count(*)
                FROM pg_stat_activity
                WHERE datname = current_database()
                  AND pid <> pg_backend_pid()
                  AND wait_event = 'advisory'
                  AND query LIKE '%pg_advisory_xact_lock%'
                """;
            if ((long)(await command.ExecuteScalarAsync() ?? 0L) >= 2)
                return;
            await Task.Delay(20);
        }
        while (DateTimeOffset.UtcNow < deadline);

        throw new TimeoutException("Two conversation creations did not reach the database barrier.");
    }

    private static async Task SetConversationCreationLockAsync(
        NpgsqlConnection connection,
        Guid creatorTenantId,
        Guid createdByMembershipId,
        Guid requestId,
        bool acquire)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = acquire
            ? """
              SELECT pg_advisory_lock(
                  hashtextextended(
                      CAST(@creatorTenantId AS text)
                      || ':' || CAST(@createdByMembershipId AS text)
                      || ':' || CAST(@requestId AS text),
                      @seed))
              """
            : """
              SELECT pg_advisory_unlock(
                  hashtextextended(
                      CAST(@creatorTenantId AS text)
                      || ':' || CAST(@createdByMembershipId AS text)
                      || ':' || CAST(@requestId AS text),
                      @seed))
              """;
        command.Parameters.AddWithValue("creatorTenantId", creatorTenantId);
        command.Parameters.AddWithValue("createdByMembershipId", createdByMembershipId);
        command.Parameters.AddWithValue("requestId", requestId);
        command.Parameters.AddWithValue("seed", ConversationCreationLockSeed);
        await command.ExecuteNonQueryAsync();
    }

    protected override void OnReset(IServiceScope scope)
    {
        connectionString = scope.ServiceProvider
            .GetRequiredService<IConfiguration>()
            .GetConnectionString(B2BDb.Name)
            ?? throw new InvalidOperationException($"Connection string '{B2BDb.Name}' is required.");
    }
}
