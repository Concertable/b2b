using Npgsql;
using Respawn;
using Respawn.Graph;
using Testcontainers.PostgreSql;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal sealed class B2BPostgresFixture : IAsyncDisposable
{
    private const string PostgisImage = "postgis/postgis:17-3.5";

    private PostgreSqlContainer? container;
    private NpgsqlConnection? connection;
    private Respawner? respawner;

    public string ConnectionString => (container ?? throw NotStarted()).GetConnectionString();

    public async Task InitializeAsync()
    {
        container = new PostgreSqlBuilder()
            .WithImage(PostgisImage)
            .WithCommand("-c", "max_prepared_transactions=100")
            .Build();
        await container.StartAsync();
        connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
    }

    public async Task InitializeRespawnerAsync(IReadOnlyCollection<string> schemas)
    {
        var database = connection ?? throw NotStarted();
        var owned = Concertable.Testing.Integration.OwnedSchemaSelector.Select(
            schemas,
            await ReadSchemaCatalogAsync(database));

        respawner = await Respawner.CreateAsync(database, new RespawnerOptions
        {
            SchemasToInclude = [.. owned],
            TablesToIgnore =
            [
                .. Concertable.Testing.Integration.OwnedSchemaSelector.TablesToIgnore(owned),
                new Table("user", "Users"),
            ],
            DbAdapter = DbAdapter.Postgres,
            WithReseed = true,
        });
    }

    public Task ResetAsync() =>
        (respawner ?? throw new InvalidOperationException("The respawner has not been initialized."))
        .ResetAsync(connection ?? throw NotStarted());

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
            await connection.DisposeAsync();
        if (container is not null)
            await container.DisposeAsync();
    }

    private static async Task<IReadOnlyList<string>> ReadSchemaCatalogAsync(NpgsqlConnection database)
    {
        await using var command = database.CreateCommand();
        command.CommandText = "SELECT nspname FROM pg_catalog.pg_namespace";
        await using var reader = await command.ExecuteReaderAsync();
        List<string> catalog = [];
        while (await reader.ReadAsync())
            catalog.Add(reader.GetString(0));
        return catalog;
    }

    private static InvalidOperationException NotStarted() =>
        new("The PostgreSQL fixture has not been initialized.");
}
