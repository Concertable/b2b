using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Testing;
using Concertable.Auth.Hosting;
using Concertable.B2B.Hosting;
using Concertable.B2B.TestKit;
using Concertable.E2E;
using Concertable.Payment.Hosting;
using Concertable.Payment.TestKit;
using Concertable.Search.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stripe;
using System.Net;
using System.Net.Http.Headers;

namespace Concertable.B2B.E2ETests;

public sealed class AppFixture : IAsyncLifetime
{
    private const string SearchWebImage = "ghcr.io/concertable/search-web";
    private const string SearchWebDigest = "sha256:5bfb93f03c875d2adb5bbd18499f2ff11ef9a71902cd7f62811cb0d7876976cb";
    private const string SearchWorkersImage = "ghcr.io/concertable/search-workers";
    private const string SearchWorkersDigest = "sha256:c0c7d64a4b2702a0186963472ab8bf4030c2cba873748eb8fdb6be9905c84d11";
    private const string SearchMigrationsImage = "ghcr.io/concertable/search-migrations";
    private const string SearchMigrationsDigest = "sha256:0ac571000b44f5683efa6890b23b9ef5d8b9e1cb5aa3461d314ececdccd453ea";
    private const string AuthE2EDigest = "sha256:53d75c608f3d51afc52ef3e1462be6ef79809634b196832bae45cf9ac132bbd4";
    private const string PaymentE2EWebDigest = "sha256:df33de77f2d01558f9ffb3b0d1cc68ddcd26e41f6d54f65045caf3e466b4a775";
    private const string PaymentE2EWorkersDigest = "sha256:4385c505153cca1df16983864b0c99807537b37f8aea801d434092cce47c87c8";

    private DistributedApplication app = null!;
    private AspireResourceLogger resourceLogger = null!;
    private HealthWaiter healthWaiter = null!;
    private HttpClient b2bAdminClient = null!;
    private HttpClient paymentAdminClient = null!;
    private B2BTestClient b2bTestClient = null!;
    private readonly ILoggerFactory loggerFactory;
    private readonly ILogger<AppFixture> logger;
    private readonly IConfiguration configuration;
    private readonly TestTokenMinter tokenMinter;
    private readonly string authUrl;
    private readonly SemaphoreSlim resetGate = new(1, 1);
    private PayoutAccountDb payoutAccounts = null!;


    public string B2BWebUrl { get; }
    public string SearchWebUrl { get; }
    public string PaymentWebUrl { get; }
    public string AuthUrl => authUrl;
    public string VenueSpaUrl { get; }
    public string ArtistSpaUrl { get; }
    public string BusinessSpaUrl { get; }
    public HttpClient B2BClient { get; private set; } = null!;
    public HttpClient SearchClient { get; private set; } = null!;
    public HttpClient PaymentClient { get; private set; } = null!;
    public WorkersFixture Workers { get; private set; } = null!;
    public IPollingService Polling { get; private set; } = null!;
    public PaymentIntentService StripePaymentIntents { get; private set; } = null!;
    public StripeFixture Stripe { get; private set; } = null!;
    public StripeCustomerResolver StripeCustomerResolver { get; private set; } = null!;
    public SeedState SeedState { get; private set; } = null!;
    public DbFixture DbFixture { get; private set; } = null!;

    public AppFixture()
    {
        loggerFactory = LoggerFactory.Create(b => b
            .AddSimpleConsole(o => o.SingleLine = true)
            .AddProvider(new FileLoggerProvider(Path.Combine(AppContext.BaseDirectory, "e2e-diagnostics.log")))
            .SetMinimumLevel(LogLevel.Warning)
            .AddFilter("Concertable", LogLevel.Information));
        logger = loggerFactory.CreateLogger<AppFixture>();
        Polling = new PollingService(loggerFactory.CreateLogger<PollingService>());

        configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.E2E.json"))
            .AddEnvironmentVariables()
            .Build();

        var endpoints = configuration.GetSection("Endpoints").Get<E2EEndpoints>()
            ?? throw new InvalidOperationException("Endpoints section is missing from appsettings.E2E.json.");

        B2BWebUrl = endpoints.B2BWeb;
        SearchWebUrl = endpoints.SearchWeb;
        PaymentWebUrl = endpoints.PaymentWeb;
        authUrl = endpoints.Auth;
        VenueSpaUrl = endpoints.VenueSpa;
        ArtistSpaUrl = endpoints.ArtistSpa;
        BusinessSpaUrl = endpoints.BusinessSpa;

        tokenMinter = new TestTokenMinter(configuration);
    }

    public async Task InitializeAsync()
    {
        logger.InitializingE2ETestFixture();

        healthWaiter = new HealthWaiter(loggerFactory.CreateLogger<HealthWaiter>());
        var builder = AppHost.CreateE2EBuilder<Projects.Concertable_B2B_E2ETests_Web>();
        var stripeSecretKey = builder.Configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey is not configured for the B2B E2E fixture.");
        var stripeClient = new StripeClient(stripeSecretKey);
        StripeCustomerResolver = await Concertable.Testing.E2E.StripeCustomerResolver.CreateAsync(stripeClient);
        var run = Run.Create(Profile.B2B(B2BWebUrl, SearchWebUrl, authUrl, PaymentWebUrl));

        var sql = builder.Resources.OfType<SqlServerServerResource>().Single();
        var searchDb = builder.CreateResourceBuilder(sql).AddDatabase(SearchConstants.Database);
        var authResource = builder.Resources.OfType<ServiceContainerResource>()
            .Single(resource => resource.Name == AuthConstants.Resource);
        var authBuilder = builder.CreateResourceBuilder(authResource);
        authBuilder.WithImageSHA256(AuthE2EDigest["sha256:".Length..]);
        authBuilder.WithEnvironment("Auth__VerificationBaseUrl", authBuilder.GetEndpoint("https"));
        authBuilder.WithEnvironment("RateLimiting__credential__PermitLimit", "1000");
        var authService = builder.CreateResourceBuilder((IResourceWithServiceDiscovery)authResource);
        var searchMigrations = builder.AddContainerImage(
                "search-migrations",
                SearchMigrationsImage,
                SearchMigrationsDigest)
            .WithReference(searchDb)
            .WaitFor(searchDb);
        var searchWeb = builder.AddSearchWeb(SearchWebImage, SearchWebDigest, authService, searchDb)
            .WaitForCompletion(searchMigrations);
        builder.AddSearchWorkers(SearchWorkersImage, SearchWorkersDigest, searchDb,
                builder.CreateResourceBuilder(builder.Resources.OfType<AzureServiceBusResource>().Single()))
            .WaitForCompletion(searchMigrations);

        var b2bWeb = builder.Resources.OfType<ProjectResource>()
            .Single(resource => resource.Name == B2BWeb.Name);
        var paymentWeb = builder.Resources.OfType<ServiceContainerResource>()
            .Single(resource => resource.Name == PaymentConstants.WebResource);
        builder.CreateResourceBuilder(paymentWeb)
            .WithImageSHA256(PaymentE2EWebDigest["sha256:".Length..]);
        var paymentWorkers = builder.Resources.OfType<ServiceContainerResource>()
            .Single(resource => resource.Name == PaymentConstants.WorkersResource);
        builder.CreateResourceBuilder(paymentWorkers)
            .WithImageSHA256(PaymentE2EWorkersDigest["sha256:".Length..]);
        Concertable.Testing.E2E.DistributedApplicationBuilderExtensions.PinHttpsEndpoint(
            builder, authResource, new Uri(authUrl).Port);
        Concertable.Testing.E2E.DistributedApplicationBuilderExtensions.PinHttpsEndpoint(
            builder, paymentWeb, new Uri(PaymentWebUrl).Port);
        Concertable.Testing.E2E.DistributedApplicationBuilderExtensions.PinHttpsEndpoint(
            builder, searchWeb.Resource, new Uri(SearchWebUrl).Port);
        Concertable.Testing.E2E.DistributedApplicationBuilderExtensions.PinHttpsEndpoint(
            builder, b2bWeb, new Uri(B2BWebUrl).Port);
        foreach (var wait in b2bWeb.Annotations
                     .OfType<WaitAnnotation>()
                     .Where(wait => ReferenceEquals(wait.Resource, authResource) ||
                                    ReferenceEquals(wait.Resource, paymentWeb))
                     .ToArray())
            b2bWeb.Annotations.Remove(wait);
        foreach (var resource in new[] { authResource, paymentWeb, searchWeb.Resource })
            resource.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
                context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "E2E"));
        paymentWeb.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["E2E__AdminKey"] = run.AdminKey;
            AddStripeCustomers(context, StripeCustomerResolver);
        }));
        paymentWorkers.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["DOTNET_ENVIRONMENT"] = "E2E";
            AddStripeCustomers(context, StripeCustomerResolver);
        }));
        b2bWeb.Annotations.Add(new EnvironmentCallbackAnnotation(context =>
        {
            context.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "E2E";
            context.EnvironmentVariables["E2E__AdminKey"] = run.AdminKey;
        }));
        StripePaymentIntents = new PaymentIntentService(stripeClient);
        Stripe = new StripeFixture(stripeClient);

        app = builder.Build();
        resourceLogger = new AspireResourceLogger(
            app.ResourceNotifications, app.Services.GetRequiredService<ResourceLoggerService>(), logger);
        await app.StartAsync();

        B2BClient = new HttpClient { BaseAddress = new Uri(B2BWebUrl) };
        SearchClient = new HttpClient { BaseAddress = new Uri(SearchWebUrl) };
        PaymentClient = new HttpClient { BaseAddress = new Uri(PaymentWebUrl) };
        Workers = new WorkersFixture(app, Polling);

        // WORKAROUND (TECH_DEBT.md): 12 not 6 — the 71 demo users seed via the async
        // credential-registration chain, slow on CI's ASB emulator. Revert to 6 once seed is faster.
        await healthWaiter.WaitForAllHealthyAsync(
            [B2BWebUrl, SearchWebUrl, PaymentWebUrl],
            TimeSpan.FromMinutes(12));

        payoutAccounts = new PayoutAccountDb(
            await app.GetConnectionStringAsync(PaymentConstants.Database)
                ?? throw new InvalidOperationException("Payment connection string is missing."));
        await Polling.UntilAsync(
            () => payoutAccounts.GetPayableOwnerIdsAsync(),
            payable => StripeTestAccounts.ByOwnerId.Keys.All(payable.Contains),
            timeout: TimeSpan.FromMinutes(3));

        b2bAdminClient = new HttpClient { BaseAddress = new Uri(B2BWebUrl) };
        paymentAdminClient = new HttpClient { BaseAddress = new Uri(PaymentWebUrl) };
        b2bTestClient = new B2BTestClient(
            b2bAdminClient,
            run.AdminKey);
        var paymentTestClient = new PaymentTestClient(
            paymentAdminClient,
            run.AdminKey);
        DbFixture = new DbFixture(b2bTestClient, paymentTestClient);
        await DbFixture.ResetAsync();
        SeedState = await b2bTestClient.GetSeedStateAsync();

        logger.E2ETestFixtureReady();
    }

    public async Task ResetAsync()
    {
        await resetGate.WaitAsync();
        try
        {
            logger.ResettingTestState();
            Stripe.Reset();
            await DbFixture.ResetAsync();
            // The reset replays every payout-owner registration, and an owner is briefly unusable as a
            // payer while its customer and connect account are re-provisioned. Tests that open a payment
            // session must not race that window.
            await Polling.UntilAsync(
                () => payoutAccounts.GetPayableOwnerIdsAsync(),
                payable => StripeTestAccounts.ByOwnerId.Keys.All(payable.Contains),
                timeout: TimeSpan.FromMinutes(3));
            SeedState = await b2bTestClient.GetSeedStateAsync();
        }
        finally
        {
            resetGate.Release();
        }
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var token = await tokenMinter.MintAsync(email, SeedState.TestPassword);
        var client = new HttpClient { BaseAddress = new Uri(B2BWebUrl) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task CommitArtistPaymentMethodAsync(HttpClient artistClient, int opportunityId)
    {
        var checkoutPath = $"/api/application/opportunity/{opportunityId}/checkout";
        var response = await artistClient.PostAsync(checkoutPath);
        await response.ShouldBe(HttpStatusCode.OK);
        var checkout = await response.Content.ReadAsync<B2BCheckoutState>()
            ?? throw new InvalidOperationException($"{checkoutPath} returned an empty checkout.");
        await Stripe.ConfirmPaymentMethodAsync(checkout.Session.ClientSecret);
    }

    public async Task CommitVenuePaymentMethodAsync(int applicationId) =>
        await Stripe.ConfirmPaymentMethodAsync(
            await b2bTestClient.OpenMethodVerificationAsync(applicationId));

    public Task WaitForTokenMintingAsync(string email, string password) =>
        tokenMinter.WaitUntilMintableAsync(email, password, Polling);

    public async Task DisposeAsync()
    {
        try
        {
            B2BClient?.Dispose();
            SearchClient?.Dispose();
            PaymentClient?.Dispose();
            b2bAdminClient?.Dispose();
            paymentAdminClient?.Dispose();
            Workers?.Dispose();
            tokenMinter.Dispose();
            healthWaiter?.Dispose();
            if (app is not null)
                await app.DisposeAsync();
            if (resourceLogger is not null)
                await resourceLogger.DisposeAsync();
        }
        finally
        {
            try
            {
                if (StripeCustomerResolver is not null)
                    await StripeCustomerResolver.DisposeAsync();
            }
            finally
            {
                resetGate.Dispose();
                loggerFactory.Dispose();
            }
        }
    }

    public ResourceNotificationService ResourceNotifications => app.ResourceNotifications;

    private static void AddStripeCustomers(
        EnvironmentCallbackContext context,
        StripeCustomerResolver stripeCustomers)
    {
        foreach (var (key, value) in stripeCustomers.GetConfiguration())
            context.EnvironmentVariables[key.Replace(":", "__", StringComparison.Ordinal)] = value;
    }

    // AddNpmApp has no HTTP readiness check, so Aspire reports a SPA 'Running' before its Vite dev
    // server actually serves. UI runs must gate on real serving (polls until 200, throws on timeout)
    // before driving a browser at it — otherwise the first navigation races Vite's startup.
    public Task WaitForSpasServingAsync(TimeSpan timeout) =>
        healthWaiter.WaitForAllServingAsync([VenueSpaUrl, ArtistSpaUrl, BusinessSpaUrl], timeout);

}
