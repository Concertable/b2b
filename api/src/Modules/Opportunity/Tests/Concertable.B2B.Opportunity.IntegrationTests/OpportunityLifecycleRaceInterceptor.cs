using System.Data.Common;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Concertable.B2B.Opportunity.IntegrationTests;

internal sealed class OpportunityLifecycleRaceInterceptor : DbCommandInterceptor, IResettable
{
    private readonly Lock gate = new();
    private Func<Task>? competingChange;
    private TaskCompletionSource<int> competingBackend = NewSignal<int>();
    private NpgsqlDataSource? dataSource;
    private bool awaitingCompetingLock;

    public void UseDataSource(NpgsqlDataSource value)
    {
        lock (gate)
            dataSource = value;
    }

    public void ArmOnce(Func<Task> change)
    {
        lock (gate)
        {
            competingChange = change;
            competingBackend = NewSignal<int>();
        }
    }

    public async Task WaitForCompetingLockWaitAsync(CancellationToken ct = default)
    {
        var processId = await competingBackend.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        NpgsqlDataSource observer;
        lock (gate)
            observer = dataSource ?? throw new InvalidOperationException("The database observer is not configured.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await using var connection = await observer.OpenConnectionAsync(timeout.Token);
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE pid = @processId AND wait_event_type = 'Lock')";
            command.Parameters.AddWithValue("processId", processId);

            while (await command.ExecuteScalarAsync(timeout.Token) is not true)
                await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("The competing Opportunity command did not enter a PostgreSQL lock wait.");
        }
    }

    public void Reset()
    {
        lock (gate)
        {
            competingChange = null;
            awaitingCompetingLock = false;
            competingBackend.TrySetCanceled();
            competingBackend = NewSignal<int>();
        }
    }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Func<Task>? change = null;
        lock (gate)
        {
            CaptureCompetingBackend(command);

            if (competingChange is not null && IsOpportunityUpdate(command))
            {
                change = competingChange;
                competingChange = null;
                awaitingCompetingLock = true;
            }
        }

        try
        {
            if (change is not null)
                await RunDetachedAsync(change);
        }
        finally
        {
            if (change is not null)
            {
                lock (gate)
                    awaitingCompetingLock = false;
            }
        }

        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            CaptureCompetingBackend(command);
        }

        return ValueTask.FromResult(result);
    }

    private static bool IsOpportunityUpdate(DbCommand command) =>
        command.CommandText.Contains("UPDATE opportunity.\"Opportunities\"", StringComparison.Ordinal);

    private static bool IsOpportunityLock(DbCommand command) =>
        command.CommandText.Contains("FROM opportunity.\"Opportunities\"", StringComparison.Ordinal)
        && command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase);

    private void CaptureCompetingBackend(DbCommand command)
    {
        if (awaitingCompetingLock
            && IsOpportunityLock(command)
            && command.Connection is NpgsqlConnection connection)
            competingBackend.TrySetResult(connection.ProcessID);
    }

    private static Task RunDetachedAsync(Func<Task> change)
    {
        using (ExecutionContext.SuppressFlow())
            return Task.Run(change);
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
