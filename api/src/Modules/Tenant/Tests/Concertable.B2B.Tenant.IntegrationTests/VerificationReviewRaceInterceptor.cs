using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Concertable.B2B.Tenant.IntegrationTests;

internal sealed class VerificationReviewRaceInterceptor : DbCommandInterceptor, IResettable
{
    private readonly Lock gate = new();
    private readonly HashSet<int> backends = [];
    private TaskCompletionSource<int[]> competingBackends = NewSignal();
    private NpgsqlDataSource? dataSource;
    private bool armed;

    public void UseDataSource(NpgsqlDataSource value)
    {
        lock (gate)
            dataSource = value;
    }

    public void Arm()
    {
        lock (gate)
        {
            backends.Clear();
            competingBackends = NewSignal();
            armed = true;
        }
    }

    public async Task WaitForCompetingLockWaitsAsync(CancellationToken ct = default)
    {
        var processIds = await competingBackends.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        NpgsqlDataSource observer;
        lock (gate)
            observer = dataSource ?? throw new InvalidOperationException("The database observer is not configured.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            await using var connection = await observer.OpenConnectionAsync(timeout.Token);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM pg_stat_activity
                WHERE pid IN (@firstProcessId, @secondProcessId)
                  AND wait_event_type = 'Lock'
                """;
            command.Parameters.AddWithValue("firstProcessId", processIds[0]);
            command.Parameters.AddWithValue("secondProcessId", processIds[1]);

            while (Convert.ToInt32(await command.ExecuteScalarAsync(timeout.Token)) < 2)
                await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("The competing verification reviews did not enter PostgreSQL lock waits.");
        }
    }

    public void Reset()
    {
        lock (gate)
        {
            armed = false;
            backends.Clear();
            competingBackends.TrySetCanceled();
            competingBackends = NewSignal();
        }
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        lock (gate)
        {
            if (armed
                && IsVerificationLock(command)
                && command.Connection is NpgsqlConnection connection
                && backends.Add(connection.ProcessID)
                && backends.Count == 2)
            {
                armed = false;
                competingBackends.TrySetResult([.. backends]);
            }
        }

        return ValueTask.FromResult(result);
    }

    private static bool IsVerificationLock(DbCommand command) =>
        command.CommandText.Contains("FROM tenant.\"Verifications\"", StringComparison.Ordinal)
        && command.CommandText.Contains("FOR UPDATE", StringComparison.OrdinalIgnoreCase);

    private static TaskCompletionSource<int[]> NewSignal() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
