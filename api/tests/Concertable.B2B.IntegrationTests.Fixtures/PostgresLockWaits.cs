using Npgsql;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public static class PostgresLockWaits
{
    public static async Task WaitForWaiterAsync(string connectionString, string queryPattern)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(timeout.Token);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM pg_stat_activity
            WHERE pid <> pg_backend_pid()
              AND wait_event_type = 'Lock'
              AND query LIKE @queryPattern
            """;
        command.Parameters.AddWithValue("queryPattern", queryPattern);
        try
        {
            while (Convert.ToInt32(await command.ExecuteScalarAsync(timeout.Token)) == 0)
                await Task.Delay(TimeSpan.FromMilliseconds(25), timeout.Token);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            throw new TimeoutException($"No PostgreSQL session entered a lock wait on {queryPattern}.");
        }
    }
}
