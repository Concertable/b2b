using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Concertable.Auth.Hosting;
using Concertable.B2B.Admin.Contracts;
using Concertable.B2B.Hosting;
using Concertable.B2B.Seed.Simulator;
using Concertable.B2B.Web;
using Concertable.B2B.Workers;
using Concertable.Payment.Hosting;
using Concertable.Testing.Architecture;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.False(jwtOptions.RequireHttpsMetadata);
        var invalidBuilder = WebApplication.CreateBuilder(CompositionTestArguments.Create());
        invalidBuilder.AddB2BWebHost();
        invalidBuilder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => invalidBuilder.Build());
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
        var validBuilder = B2BAppHost.CreateBuilder([]);
        AssertImageEndpoint(validBuilder, AuthConstants.Resource, "https");
        AssertImageEndpoint(validBuilder, PaymentConstants.WebResource, "https");
        AssertImageEndpoint(validBuilder, PaymentConstants.WebResource, "http");
        using var app = validBuilder.Build();
        var builder = B2BAppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Fact]
    public void AppHost_PublishGraphWithStripeCli_IsValid()
    {
        var builder = B2BAppHost.CreateBuilder(
            ["--publisher", "manifest", "--Stripe:SecretKey=sk_test_composition"]);

        Assert.True(builder.ExecutionContext.IsPublishMode);
        Assert.Single(builder.Resources, resource => resource.Name == PaymentConstants.StripeCliResource);
        using var app = builder.Build();
    }

    [Fact]
    public void LocalSpaSurfaces_AreCanonicalAndCollisionFree()
    {
        LocalSpaSurface[] expected =
        [
            new("customer", 5174, LocalSpaClient.Customer),
            new("venue", 5175, LocalSpaClient.Venue),
            new("artist", 5176, LocalSpaClient.Artist),
            new("business", 5177, null),
            new("admin", 5178, LocalSpaClient.Admin)
        ];

        Assert.Equal(expected, LocalSpaSurfaces.All);
        Assert.Equal(expected.Length, LocalSpaSurfaces.All.Select(surface => surface.ResourceName).Distinct().Count());
        Assert.Equal(expected.Length, LocalSpaSurfaces.All.Select(surface => surface.HttpsPort).Distinct().Count());
        Assert.Equal(
            expected.Where(surface => surface.AuthClient is not null),
            LocalSpaSurfaces.Authenticated);
        Assert.Equal(
            expected.Where(surface => surface != LocalSpaSurfaces.Customer),
            LocalSpaSurfaces.B2B);
    }

    [Fact]
    public async Task AppHost_WebSpaOrigins_AreConsistent()
    {
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
            LocalSpaSurfaces.B2B.Select(surface => surface.ResourceName).Order(),
            nodeApps.Select(resource => resource.Name).Order());

        for (var index = 0; index < LocalSpaSurfaces.B2B.Count; index++)
        {
            var surface = LocalSpaSurfaces.B2B[index];
            var nodeApp = Assert.Single(nodeApps, resource => resource.Name == surface.ResourceName);
            var endpoint = Assert.Single(nodeApp.Annotations.OfType<EndpointAnnotation>());

            Assert.Equal("https", endpoint.UriScheme);
            Assert.Equal(surface.HttpsPort, endpoint.Port);
            Assert.Equal(surface.Origin, b2bEnvironment[$"Cors__AllowedOrigins__{index}"]);

            if (surface.AuthClient is not { } authClient)
                continue;

            Assert.Equal(
                $"{surface.Origin}/auth/callback",
                authEnvironment[$"Auth__SpaClients__{authClient}__RedirectUri"]);
            Assert.Equal(surface.Origin, authEnvironment[$"Auth__SpaClients__{authClient}__PostLogoutRedirectUri"]);
            Assert.Equal(surface.Origin, authEnvironment[$"Auth__SpaClients__{authClient}__AllowedCorsOrigins__0"]);
        }
    }

    private static void AssertImageEndpoint(
        IDistributedApplicationBuilder builder,
        string resourceName,
        string endpointName)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource => resource.Name == resourceName));
        var endpoint = Assert.Single(
            resource.Annotations.OfType<EndpointAnnotation>(),
            endpoint => endpoint.Name == endpointName);

        Assert.Equal(endpointName, endpoint.Name);
        Assert.Equal("http", endpoint.UriScheme);
        Assert.Equal(8080, endpoint.TargetPort);
    }
}
