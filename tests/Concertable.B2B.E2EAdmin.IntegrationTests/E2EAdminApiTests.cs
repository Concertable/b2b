using System.Data;
using System.Net;
using Concertable.B2B.E2ETests.Server;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Concertable.B2B.E2EAdmin.IntegrationTests;

public sealed class E2EAdminApiTests
{
    private const string AdminKey = "admin-key";
    private const string AdminKeyHeader = "X-Concertable-E2E-Key";
    private const string ConnectionStringName = "B2BDb";

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddB2BE2EAdmin_BlankAdminKey_RejectsHostRegistration(string? adminKey)
    {
        var builder = E2EAdminTestHost.CreateBuilder(adminKey, ConnectionStringName);

        var exception = Should.Throw<InvalidOperationException>(
            () => builder.Services.AddB2BE2EAdmin(builder.Configuration, builder.Environment));

        exception.Message.ShouldContain("E2E:AdminKey");
    }

    [Fact]
    public void AddB2BE2EAdmin_NonE2EEnvironment_RejectsHostRegistration()
    {
        var builder = E2EAdminTestHost.CreateBuilder(AdminKey, ConnectionStringName, Environments.Development);

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

    private static Task<E2EAdminTestHost> StartHostAsync() =>
        E2EAdminTestHost.StartAsync(
            AdminKey,
            ConnectionStringName,
            (services, configuration, environment) =>
            {
                services.AddSingleton<SeedState>(_ => throw new NotSupportedException());
                services.AddSingleton<IDbConnection>(_ => throw new NotSupportedException());
                services.AddB2BE2EAdmin(configuration, environment);
            },
            app => app.MapB2BE2EAdmin());
}
