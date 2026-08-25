using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Hosting;
using Concertable.B2B.Admin.Contracts;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Hosting;
using Concertable.B2B.Seed.Simulator;
using Concertable.B2B.Web;
using Concertable.B2B.Workers;
using Concertable.Messaging.Application;
using Concertable.Testing.Architecture;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Concertable.B2B.ArchitectureTests;

public sealed class B2BHostGraphTests
{
    [Fact]
    public void Web_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddB2BWebHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(B2BWebHostExtensions).Assembly]
        });
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddB2BWebHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void Web_MessageTopology_HandlesDurableCommandsWithoutSelfSubscriptions()
    {
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddB2BWebHost();
        using var app = builder.Build();
        var registry = app.Services.GetRequiredService<MessageTypeRegistry>();

        Assert.Contains(typeof(NotifyConcertDraftCreatedCommand), registry.HandledCommandTypes);
        Assert.DoesNotContain(typeof(BookingCancelledEvent), registry.SubscribedEventTypes);
        Assert.DoesNotContain(typeof(ConcertCancelledEvent), registry.SubscribedEventTypes);
        Assert.DoesNotContain(typeof(ConcertCreatedEvent), registry.SubscribedEventTypes);
    }

    [Fact]
    public void Functions_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = B2BWorkerHost.CreateBuilder(CompositionTestArguments.Create());
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(B2BWorkerHost).Assembly],
            IsFunction = method => method.IsDefined(typeof(FunctionAttribute), inherit: false)
        });
        var invalidBuilder = B2BWorkerHost.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void Web_MissingAdminModule_FailsWithUnresolvedDependency()
    {
        // IAdminModule's only consumer is UserController.Me() — Web-hosted, not Workers.
        var builder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        builder.AddB2BWebHost();
        builder.Services.RemoveAll<IAdminModule>();
        var exception = Record.Exception(() =>
        {
            using var app = builder.Build();
            builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
            {
                RootAssemblies = [typeof(B2BWebHostExtensions).Assembly]
            });
        });
        Assert.NotNull(exception);
        Assert.Contains(typeof(IAdminModule).FullName!, exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void SeedSimulator_ProductionGraphAndStrictValidation_AreValid()
    {
        var builder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        builder.AddSeedSimulatorHost();
        using var app = builder.Build();
        builder.Services.ValidateComposition(app.Services, new CompositionValidationOptions
        {
            RootAssemblies = [typeof(HostExtensions).Assembly]
        });
        var invalidBuilder = Host.CreateApplicationBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddSeedSimulatorHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
    }

    [Fact]
    public void AppHost_ProductionGraphAndStrictValidation_AreValid()
    {
        using var app = B2BAppHost.CreateBuilder([]).Build();
        var builder = B2BAppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Fact]
    public async Task AppHost_WebSpaOrigins_AreConsistent()
    {
        (string Surface, int Port, string? AuthClient)[] surfaces =
        [
            ("venue", 5175, "Venue"),
            ("artist", 5176, "Artist"),
            ("business", 5177, null),
            ("admin", 5178, "Admin")
        ];

        var builder = B2BAppHost.CreateBuilder([]);
        var nodeApps = builder.Resources.OfType<NodeAppResource>().ToArray();
        var auth = Assert.IsAssignableFrom<IResourceWithEnvironment>(
            builder.Resources.Single(resource => resource.Name == AuthConstants.Resource));
        var b2b = Assert.IsAssignableFrom<IResourceWithEnvironment>(
            builder.Resources.Single(resource => resource.Name == B2BConstants.WebResource));
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var authConfiguration = await ExecutionConfigurationBuilder.Create(auth)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(executionContext, NullLogger.Instance, CancellationToken.None);
        var b2bConfiguration = await ExecutionConfigurationBuilder.Create(b2b)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(executionContext, NullLogger.Instance, CancellationToken.None);
        var authEnvironment = authConfiguration.EnvironmentVariables.ToDictionary();
        var b2bEnvironment = b2bConfiguration.EnvironmentVariables.ToDictionary();

        Assert.Equal(
            surfaces.Select(surface => surface.Surface).Order(),
            nodeApps.Select(resource => resource.Name).Order());

        foreach (var (surface, port, authClient) in surfaces)
        {
            var origin = $"https://localhost:{port}";
            var nodeApp = Assert.Single(nodeApps, resource => resource.Name == surface);
            var endpoint = Assert.Single(nodeApp.Annotations.OfType<EndpointAnnotation>());

            Assert.Equal("https", endpoint.UriScheme);
            Assert.Equal(port, endpoint.Port);
            Assert.Contains(origin, b2bEnvironment.Values);

            if (authClient is null)
                continue;

            Assert.Equal(
                $"{origin}/auth/callback",
                authEnvironment[$"Auth__SpaClients__{authClient}__RedirectUri"]);
            Assert.Equal(origin, authEnvironment[$"Auth__SpaClients__{authClient}__PostLogoutRedirectUri"]);
            Assert.Equal(origin, authEnvironment[$"Auth__SpaClients__{authClient}__AllowedCorsOrigins__0"]);
        }
    }
}
