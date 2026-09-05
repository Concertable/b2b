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
    public void Web_ProductionEnvironment_RequiresHttpsMetadata()
    {
        var arguments = CompositionTestArguments.Create();
        arguments[0] = "--environment=Production";
        var builder = WebApplication.CreateBuilder(arguments);
        builder.AddB2BWebHost();
        using var app = builder.Build();
        var jwtOptions = app.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.True(jwtOptions.RequireHttpsMetadata);
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
    public async Task AppHost_ProductionGraphAndStrictValidation_AreValid()
    {
        var validBuilder = AppHost.CreateBuilder([]);
        AssertImageEndpoint(validBuilder, AuthConstants.Resource, "https", scheme: "https");
        AssertContainerRuntimeArgs(validBuilder, AuthConstants.Resource, "--user", "root");
        AssertUsesDeveloperCertificate(validBuilder, AuthConstants.Resource);
        AssertImageEndpoint(validBuilder, PaymentConstants.WebResource, "https");
        AssertImageEndpoint(validBuilder, PaymentConstants.WebResource, "http");
        Assert.DoesNotContain(validBuilder.Resources.OfType<NodeAppResource>(),
            resource => resource.Name.StartsWith("mobile-", StringComparison.Ordinal));
        Assert.DoesNotContain(validBuilder.Resources, resource => resource.Name == "b2b-dev");
        var auth = validBuilder.Resources.Single(resource => resource.Name == AuthConstants.Resource);
        var authEnvironment = await GetRawEnvironmentAsync(auth, CancellationToken.None);
        Assert.DoesNotContain("Auth__PublicUrl", authEnvironment.Keys);
        using var app = validBuilder.Build();
        var builder = AppHost.CreateBuilder([]);
        builder.Services.AddInvalidLifetimeGraph();
        Assert.ThrowsAny<Exception>(() => builder.Build());
    }

    [Fact]
    public void AppHost_PublishGraphWithStripeCli_IsValid()
    {
        var builder = AppHost.CreateBuilder(
            ["--publisher", "manifest", "--Stripe:SecretKey=sk_test_composition"]);

        Assert.True(builder.ExecutionContext.IsPublishMode);
        Assert.Single(builder.Resources, resource => resource.Name == PaymentConstants.StripeCliResource);
        using var app = builder.Build();
    }

    [Fact]
    public async Task AppHost_MobileGraph_ContainsOnlyB2BSurfaces()
    {
        var builder = AppHost.CreateBuilder(["--RunMobile=true"]);
        Assert.Equal(
            new[] { "admin", "artist", "business", "mobile-b2b", "venue" },
            builder.Resources.OfType<NodeAppResource>().Select(resource => resource.Name).Order());
        AssertNodeAppDirectory(builder, "venue", "app", "web", "b2b", "venue");
        AssertNodeAppDirectory(builder, "artist", "app", "web", "b2b", "artist");
        AssertNodeAppDirectory(builder, "business", "app", "web", "b2b", "business");
        AssertNodeAppDirectory(builder, "admin", "app", "web", "admin");
        AssertNodeAppDirectory(builder, "mobile-b2b", "app", "mobile", "b2b");
        Assert.Single(builder.Resources, resource => resource.Name == "b2b-dev");
        Assert.DoesNotContain(builder.Resources, resource => resource.Name is "customer-web" or "search-web");
        using var app = builder.Build();

        AllocateTunnelEndpoints(builder, "b2b-dev");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var mobile = Assert.Single(builder.Resources.OfType<NodeAppResource>(), resource => resource.Name == "mobile-b2b");
        AssertClearMetroCacheCommand(mobile);
        var mobileEnvironment = await GetResolvedEnvironmentAsync(mobile, cancellation.Token);
        Assert.Equal("localhost", mobileEnvironment["REACT_NATIVE_PACKAGER_HOSTNAME"]);
        AssertTunnelUrl(mobileEnvironment, "EXPO_PUBLIC_API_URL", "b2b-dev-b2b-web-http");
        AssertTunnelUrl(mobileEnvironment, "EXPO_PUBLIC_AUTH_AUTHORITY", "b2b-dev-auth-https");
        AssertTunnelUrl(mobileEnvironment, "EXPO_PUBLIC_PAYMENT_API_URL", "b2b-dev-payment-web-https");
        Assert.DoesNotContain("EXPO_PUBLIC_CUSTOMER_API_URL", mobileEnvironment.Keys);
        Assert.DoesNotContain("EXPO_PUBLIC_SEARCH_API_URL", mobileEnvironment.Keys);

        var auth = builder.Resources.Single(resource => resource.Name == AuthConstants.Resource);
        var authEnvironment = await GetRawEnvironmentAsync(auth, cancellation.Token);
        await AssertTunnelValueAsync(authEnvironment, "Auth__PublicUrl", "b2b-dev-auth-https", cancellation.Token);
    }

    [Fact]
    public void LocalSpaSurfaces_AreCanonicalAndCollisionFree()
    {
        SpaSurface[] expected =
        [
            new("venue", 5175, "Venue"),
            new("artist", 5176, "Artist"),
            new("business", 5177, null),
            new("admin", 5178, "Admin")
        ];

        Assert.Equal(expected, B2BLocalSpaSurfaces.All);
        Assert.Equal(expected.Length, B2BLocalSpaSurfaces.All.Select(surface => surface.ResourceName).Distinct().Count());
        Assert.Equal(expected.Length, B2BLocalSpaSurfaces.All.Select(surface => surface.HttpsPort).Distinct().Count());
    }

    [Fact]
    public async Task AppHost_WebSpaOrigins_AreConsistent()
    {
        var builder = AppHost.CreateBuilder([]);
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
            new[] { "Venue", "Artist", "Admin" },
            authEnvironment
                .Where(pair => pair.Key.StartsWith("Auth__SpaClients__EnabledClients__", StringComparison.Ordinal))
                .OrderBy(pair => pair.Key)
                .Select(pair => pair.Value));

        Assert.Equal(
            B2BLocalSpaSurfaces.All.Select(surface => surface.ResourceName).Order(),
            nodeApps.Select(resource => resource.Name).Order());

        for (var index = 0; index < B2BLocalSpaSurfaces.All.Count; index++)
        {
            var surface = B2BLocalSpaSurfaces.All[index];
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

    private static void AssertContainerRuntimeArgs(
        IDistributedApplicationBuilder builder,
        string resourceName,
        params object[] expected)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource => resource.Name == resourceName));
        var args = new List<object>();
        foreach (var annotation in resource.Annotations.OfType<ContainerRuntimeArgsCallbackAnnotation>())
            annotation.Callback(new ContainerRuntimeArgsCallbackContext(args, CancellationToken.None))
                .GetAwaiter()
                .GetResult();

        Assert.Equal(expected, args);
    }

    private static void AllocateTunnelEndpoints(IDistributedApplicationBuilder builder, string tunnelName)
    {
        var ports = builder.Resources.OfType<Aspire.Hosting.DevTunnels.DevTunnelPortResource>()
            .Where(port => port.Name.StartsWith(tunnelName + "-", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(ports);
        foreach (var port in ports)
            foreach (var endpoint in port.Annotations.OfType<EndpointAnnotation>())
                endpoint.AllocatedEndpoint = new AllocatedEndpoint(endpoint, $"{port.Name}.example.test", 443);
    }

    private static void AssertNodeAppDirectory(
        IDistributedApplicationBuilder builder,
        string resourceName,
        params string[] relativePath)
    {
        var repoRoot = new DirectoryInfo(builder.AppHostDirectory);
        while (repoRoot is not null && !Directory.Exists(Path.Combine(repoRoot.FullName, "app")))
            repoRoot = repoRoot.Parent;

        Assert.NotNull(repoRoot);
        var expected = Path.GetFullPath(Path.Combine([repoRoot.FullName, .. relativePath]));
        var resource = Assert.Single(builder.Resources.OfType<NodeAppResource>(), resource => resource.Name == resourceName);
        var actual = Path.GetFullPath(resource.WorkingDirectory);
        Assert.True(string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase),
            $"Expected '{resourceName}' to use '{expected}', but it uses '{actual}'.");
        Assert.True(Directory.Exists(actual), $"Frontend directory '{actual}' does not exist.");
    }

    private static void AssertClearMetroCacheCommand(NodeAppResource mobile)
    {
        var command = Assert.Single(mobile.Annotations.OfType<ResourceCommandAnnotation>(),
            command => command.Name == "clear-metro-cache");
        Assert.Equal("Clear Metro Cache", command.DisplayName);
        Assert.Equal("ArrowCounterclockwise", command.IconName);
    }

    private static async Task<Dictionary<string, string>> GetResolvedEnvironmentAsync(
        IResource resource, CancellationToken cancellationToken)
    {
        var environmentResource = Assert.IsAssignableFrom<IResourceWithEnvironment>(resource);
        var configuration = await ExecutionConfigurationBuilder.Create(environmentResource)
            .WithEnvironmentVariablesConfig()
            .BuildAsync(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
                NullLogger.Instance, cancellationToken);
        return configuration.EnvironmentVariables.ToDictionary();
    }

    private static async Task<Dictionary<string, object>> GetRawEnvironmentAsync(
        IResource resource, CancellationToken cancellationToken)
    {
        var environment = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(
            new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run),
            resource, environment, cancellationToken);
        foreach (var annotation in resource.Annotations.OfType<EnvironmentCallbackAnnotation>().ToArray())
            await annotation.Callback(context);
        return environment;
    }

    private static void AssertTunnelUrl(
        IReadOnlyDictionary<string, string> environment,
        string key,
        string endpointName)
    {
        Assert.True(environment.TryGetValue(key, out var url),
            $"Missing '{key}'. Available keys: {string.Join(", ", environment.Keys.Order())}");
        Assert.Equal($"https://{endpointName}.example.test:443", url);
    }

    private static async Task AssertTunnelValueAsync(
        IReadOnlyDictionary<string, object> environment,
        string key,
        string endpointName,
        CancellationToken cancellationToken)
    {
        var url = await Assert.IsAssignableFrom<IValueProvider>(environment[key]).GetValueAsync(cancellationToken);
        Assert.Equal($"https://{endpointName}.example.test:443", url);
    }

#pragma warning disable ASPIRECERTIFICATES001 // experimental API; asserts the temporary Auth image bridge
    private static void AssertUsesDeveloperCertificate(
        IDistributedApplicationBuilder builder,
        string resourceName)
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource => resource.Name == resourceName));
        var certificate = Assert.Single(resource.Annotations.OfType<HttpsCertificateAnnotation>());

        Assert.True(certificate.UseDeveloperCertificate);
    }
#pragma warning restore ASPIRECERTIFICATES001

    private static void AssertImageEndpoint(
        IDistributedApplicationBuilder builder,
        string resourceName,
        string endpointName,
        string scheme = "http")
    {
        var resource = Assert.IsType<ServiceContainerResource>(
            builder.Resources.Single(resource => resource.Name == resourceName));
        var endpoint = Assert.Single(
            resource.Annotations.OfType<EndpointAnnotation>(),
            endpoint => endpoint.Name == endpointName);

        Assert.Equal(endpointName, endpoint.Name);
        Assert.Equal(scheme, endpoint.UriScheme);
        Assert.Equal(8080, endpoint.TargetPort);
    }
}
