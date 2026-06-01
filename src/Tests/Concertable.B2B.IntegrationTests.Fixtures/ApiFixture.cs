using Concertable.Kernel.Notifications;
using Concertable.Payment.Contracts;
using Concertable.Payment.Domain;
using Concertable.Payment.Client;
using Concertable.Payment.Application.Interfaces;
using Concertable.Payment.Application.Interfaces.Webhook;
using Concertable.Payment.Infrastructure.Data;
using Concertable.Payment.Infrastructure.Extensions;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Identity;
using Concertable.B2B.User.Domain;
using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Logging;
using Concertable.Testing.Integration.Mocks;
using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Contract.Infrastructure.Extensions;
using Concertable.B2B.User.Infrastructure.Extensions;
using Concertable.B2B.Venue.Infrastructure.Extensions;
using Concertable.B2B.Conversations.Infrastructure.Extensions;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Infrastructure;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Seed.Infrastructure.Fakers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;
using Concertable.Shared.Geocoding.Application;
using Concertable.Shared.Imaging.Application;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.B2B.DataAccess;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public sealed class ApiFixture : IAsyncLifetime
{
    private SqlFixture sqlFixture = null!;
    private WebApplicationFactory<Program> factory = null!;
    private IServiceScope? scope;
    private PaymentDbContext paymentDbContext = null!;
    private readonly XunitOutputAccessor outputAccessor = new();

    public void AttachOutput(ITestOutputHelper output) => outputAccessor.Output = output;
    public void DetachOutput() => outputAccessor.Output = null;

    public IMockNotificationClient NotificationService { get; } = new MockNotificationClient();
    public MockStripeApiClient StripeApiClient { get; } = new MockStripeApiClient();
    public IMockEmailSender EmailSender { get; } = new MockEmailSender();
    public IWebhookSimulator StripeClient { get; private set; } = null!;
    public SeedState SeedState { get; private set; } = null!;
    public IReadDbContext ReadDbContext { get; private set; } = null!;
    public IQueryable<EscrowEntity> Escrows => paymentDbContext.Escrows.AsNoTracking();

    public async Task InitializeAsync()
    {
        sqlFixture = new SqlFixture();
        await sqlFixture.InitializeAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:B2BDb"] = sqlFixture.ConnectionString,
                    ["ConnectionStrings:PaymentDb"] = sqlFixture.ConnectionString,
                    ["ExternalServices:UseRealStripe"] = "false",
                    ["ExternalServices:UseRealBlob"] = "false",
                    ["ExternalServices:UseRealEmail"] = "false",
                    ["Urls:Frontend"] = "https://localhost:5173",
                    ["BlobStorage:ContainerName"] = "images",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddLogging(b =>
                {
                    b.ClearProviders();
                    b.AddProvider(new XunitLoggerProvider(outputAccessor));
                    b.SetMinimumLevel(LogLevel.Information);
                });

                var asbDescriptors = services
                    .Where(d => d.ServiceType == typeof(IHostedService) &&
                                d.ImplementationType?.Name == "AzureServiceBusReceiver")
                    .ToList();
                foreach (var d in asbDescriptors)
                    services.Remove(d);
                services.Replace(ServiceDescriptor.Singleton<IBusTransport, MockBusTransport>());
                services.AddScoped<IStripeAccountClient, MockStripeAccountClient>();
                services.AddScoped<IStripeHoldClient, MockStripeHoldClient>();
                services.AddSingleton<INotificationClient>(NotificationService);
                services.AddSingleton(StripeApiClient);
                services.AddSingleton<IStripeApiClient>(StripeApiClient);
                services.AddKeyedScoped<IStripePaymentIntentClient, MockStripePaymentIntentClient>(PaymentSession.OnSession);
                services.AddKeyedScoped<IStripePaymentIntentClient, MockStripePaymentIntentClient>(PaymentSession.OffSession);
                services.AddResettables(NotificationService, StripeApiClient, EmailSender);
                services.AddSingleton<IEmailSender>(EmailSender);

                services.AddSingleton<PaymentConfigurationProvider>();
                services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<PaymentConfigurationProvider>());
                services.AddDbContext<PaymentDbContext>((sp, opts) =>
                    opts.UseSqlServer(sqlFixture.ConnectionString)
                        .AddInterceptors(
                            sp.GetRequiredService<AuditInterceptor>(),
                            sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                        .UseSeedingSupport(sp));
                services.AddScoped<IManagerPaymentClient, MockManagerPaymentClient>();
                services.AddScoped<IEscrowClient, MockEscrowClient>();

                services.AddSingleton<IWebhookSimulator, MockWebhookSimulator>();
                services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(_ => new WebApplicationHttpClientFactory(factory)));
                services.AddScoped<IGeocodingService, MockGeocodingService>();
                services.AddScoped<IImageService, MockImageService>();
                services.AddScoped<IDbInitializer, TestDbInitializer>();
                services.AddSeedingInfrastructure();
                services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatchInterceptor, SeedingDomainEventDispatchInterceptor>());
                services.AddSingleton<SeedCatalog>();
                services.AddScoped<SeedState>();
                services.AddScoped<ILocationFaker, LocationFaker>();
                services.AddUserTestSeeder();
                services.AddArtistTestSeeder();
                services.AddVenueTestSeeder();
                services.AddContractTestSeeder();
                services.AddConcertTestSeeder();
                services.AddPaymentTestSeeder();
                services.AddConversationsTestSeeder();

                services.PostConfigure<AuthenticationOptions>(opts =>
                {
                    opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    opts.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    opts.DefaultScheme = TestAuthHandler.SchemeName;
                });
                services.AddAuthentication()
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            });
        });

        _ = factory.Services;

        await sqlFixture.InitializeRespawnerAsync();
        StripeClient = factory.Services.GetRequiredService<IWebhookSimulator>();
    }

    public async Task DisposeAsync()
    {
        scope?.Dispose();
        await factory.DisposeAsync();
        await sqlFixture.DisposeAsync();
    }

    public async Task ResetAsync()
    {
        await sqlFixture.ResetAsync();
        foreach (var resettable in factory.Services.GetServices<IResettable>())
            resettable.Reset();
        StripeClient = factory.Services.GetRequiredService<IWebhookSimulator>();

        scope?.Dispose();
        scope = factory.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await initializer.InitializeAsync();
        SeedState = scope.ServiceProvider.GetRequiredService<SeedState>();
        ReadDbContext = scope.ServiceProvider.GetRequiredService<IReadDbContext>();
        paymentDbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    }

    public IServiceProvider Services => factory.Services;

    public HttpClient CreateClient(UserEntity user)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, user.Id.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, user.Role.ToString());
        return client;
    }

    public HttpClient CreateClient(UserEntity user, Action<TestClientOptions> configure)
    {
        var options = new TestClientOptions();
        configure(options);

        var customFactory = factory.WithWebHostBuilder(b =>
        {
            if (options.Configure is not null)
                b.ConfigureAppConfiguration((_, config) => options.Configure(config));
            if (options.Services is not null)
                b.ConfigureTestServices(options.Services);
        });

        StripeClient = customFactory.Services.GetRequiredService<IWebhookSimulator>();

        var client = customFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, user.Id.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, user.Role.ToString());
        return client;
    }

    public HttpClient CreateClient(Guid userId, Role role)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role.ToString());
        return client;
    }

    public HttpClient CreateClient(Guid userId, Role role, Action<TestClientOptions> configure)
    {
        var options = new TestClientOptions();
        configure(options);

        var customFactory = factory.WithWebHostBuilder(b =>
        {
            if (options.Configure is not null)
                b.ConfigureAppConfiguration((_, config) => options.Configure(config));
            if (options.Services is not null)
                b.ConfigureTestServices(options.Services);
        });

        StripeClient = customFactory.Services.GetRequiredService<IWebhookSimulator>();

        var client = customFactory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role.ToString());
        return client;
    }

    public HttpClient CreateClient() => factory.CreateClient();
}
