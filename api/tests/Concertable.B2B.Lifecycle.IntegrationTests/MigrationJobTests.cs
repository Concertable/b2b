using Concertable.Testing.Integration;
using Npgsql;

namespace Concertable.B2B.Lifecycle.IntegrationTests;

public sealed class MigrationJobTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task RunAsync_CanMigrateCleanDatabaseTwice()
    {
        var postgres = new PostgresFixture();
        await postgres.InitializeAsync();
        try
        {
            await B2BMigrationJob.RunAsync(postgres.ConnectionString);
            await B2BMigrationJob.RunAsync(postgres.ConnectionString);

            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT table_schema || '.' || table_name
                FROM information_schema.tables
                WHERE table_name LIKE '__EFMigrationsHistory%'
                ORDER BY table_schema, table_name
                """;
            await using var reader = await command.ExecuteReaderAsync();
            var histories = new List<string>();
            while (await reader.ReadAsync())
                histories.Add(reader.GetString(0));

            Assert.Equal(
            [
                "admin.__EFMigrationsHistory",
                "application.__EFMigrationsHistory",
                "artist.__EFMigrationsHistory",
                "booking.__EFMigrationsHistory",
                "concert.__EFMigrationsHistory",
                "conversations.__EFMigrationsHistory",
                "deal.__EFMigrationsHistory",
                "messaging.__EFMigrationsHistory_Inbox",
                "messaging.__EFMigrationsHistory_Outbox",
                "opportunity.__EFMigrationsHistory",
                "tenant.__EFMigrationsHistory",
                "user.__EFMigrationsHistory",
                "venue.__EFMigrationsHistory",
            ],
            histories);
        }
        finally
        {
            await postgres.DisposeAsync();
        }
    }
}
