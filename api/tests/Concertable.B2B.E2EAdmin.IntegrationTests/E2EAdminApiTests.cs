using System.Data;
using System.Net;
using System.Net.Http.Json;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.E2ETests.Server;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Messaging.Contracts;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Concertable.B2B.E2EAdmin.IntegrationTests;

public sealed class E2EAdminApiTests
{
    private const string AdminKey = "admin-key";
    private const string AdminKeyHeader = "X-Concertable-E2E-Key";
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddB2BE2EAdmin_BlankAdminKey_RejectsHostRegistration(string? adminKey)
    {
        var builder = E2EAdminTestHost.CreateBuilder(adminKey, B2BDb.Name);

        var exception = Should.Throw<InvalidOperationException>(
            () => builder.Services.AddB2BE2EAdmin(builder.Configuration, builder.Environment));

        exception.Message.ShouldContain("E2E:AdminKey");
    }

    [Fact]
    public void AddB2BE2EAdmin_NonE2EEnvironment_RejectsHostRegistration()
    {
        var builder = E2EAdminTestHost.CreateBuilder(AdminKey, B2BDb.Name, Environments.Development);

        var exception = Should.Throw<InvalidOperationException>(
            () => builder.Services.AddB2BE2EAdmin(builder.Configuration, builder.Environment));

        exception.Message.ShouldContain("E2E environment");
    }

    [Fact]
    public async Task Reset_MissingAdminKeyHeader_ReturnsNotFound()
    {
        await using var host = await StartHostAsync();

        using var response = await host.Client.PostAsync("/_e2e/reset", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Reset_BlankAdminKeyHeader_ReturnsNotFound(string supplied)
    {
        await using var host = await StartHostAsync();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/_e2e/reset");
        request.Headers.TryAddWithoutValidation(AdminKeyHeader, supplied).ShouldBeTrue();

        using var response = await host.Client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConcertId_AuthenticatedRequest_ReturnsSeededConcert()
    {
        var postgres = new PostgresFixture();
        await postgres.InitializeAsync();
        try
        {
            await using var connection = new NpgsqlConnection(postgres.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE SCHEMA concert;
                CREATE TABLE concert."Concerts" (
                    "Id" integer PRIMARY KEY,
                    "ApplicationId" integer NOT NULL
                );
                INSERT INTO concert."Concerts" ("Id", "ApplicationId") VALUES (23, 42);
                """;
            await command.ExecuteNonQueryAsync();

            await using var host = await StartHostAsync(connection);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/_e2e/applications/42/concert-id");
            request.Headers.Add(AdminKeyHeader, AdminKey);

            using var response = await host.Client.SendAsync(request);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<int>()).ShouldBe(23);
        }
        finally
        {
            await postgres.DisposeAsync();
        }
    }

    private static Task<E2EAdminTestHost> StartHostAsync(IDbConnection? connection = null) =>
        E2EAdminTestHost.StartAsync(
            AdminKey,
            B2BDb.Name,
            (services, configuration, environment) =>
            {
                services.AddSingleton<SeedState>(_ => throw new NotSupportedException());
                services.AddSingleton<IBusQuiescence, NoOpBusQuiescence>();
                if (connection is null)
                    services.AddSingleton<IDbConnection>(_ => throw new NotSupportedException());
                else
                    services.AddSingleton<IDbConnection>(connection);
                services.AddB2BE2EAdmin(configuration, environment);
            },
            app => app.MapB2BE2EAdmin());

    private sealed class NoOpBusQuiescence : IBusQuiescence
    {
        public Task PauseAsync(CancellationToken ct = default) => Task.CompletedTask;

        public Task ResumeAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
