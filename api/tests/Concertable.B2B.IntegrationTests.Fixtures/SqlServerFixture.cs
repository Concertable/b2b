using Microsoft.Data.SqlClient;
using Respawn;
using Respawn.Graph;
using Testcontainers.MsSql;
using Xunit;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? container;
    private SqlConnection? connection;
    private Respawner? respawner;

    public string ConnectionString => (container ?? throw NotStarted()).GetConnectionString();

    public async Task InitializeAsync()
    {
        container = new MsSqlBuilder().Build();
        await container.StartAsync();
        connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
    }

    public async Task InitializeRespawnerAsync()
    {
        respawner = await Respawner.CreateAsync(Connection, new RespawnerOptions
        {
            TablesToIgnore = [new Table("__EFMigrationsHistory")],
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true
        });
    }

    public async Task ResetAsync() =>
        await (respawner ?? throw new InvalidOperationException(
            $"{nameof(InitializeRespawnerAsync)} must run before {nameof(ResetAsync)}."))
            .ResetAsync(Connection);

    public async Task DisposeAsync()
    {
        if (connection is not null)
            await connection.DisposeAsync();
        if (container is not null)
            await container.DisposeAsync();
    }

    private SqlConnection Connection => connection ?? throw NotStarted();

    private static InvalidOperationException NotStarted() =>
        new($"{nameof(InitializeAsync)} must run before using the SQL Server fixture.");
}
