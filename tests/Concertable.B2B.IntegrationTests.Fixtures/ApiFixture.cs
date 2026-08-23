using Concertable.Kernel.Notifications;
using Concertable.Kernel.DependencyInjection;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Events;
using Concertable.Payment.Client;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Testing.Integration;
using Concertable.Testing.Integration.Logging;
using Concertable.Testing.Integration.Mocks;
using Concertable.B2B.Artist.Infrastructure.Extensions;
using Concertable.B2B.Application.Infrastructure.Extensions;
using Concertable.B2B.Booking.Infrastructure.Extensions;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.B2B.Deal.Infrastructure.Extensions;
using Concertable.B2B.Tenant.Infrastructure.Extensions;
using Concertable.B2B.Admin.Infrastructure.Extensions;
using Concertable.B2B.User.Infrastructure.Extensions;
using Concertable.B2B.Venue.Infrastructure.Extensions;
using Concertable.B2B.Conversations.Infrastructure.Extensions;
using Concertable.B2B.Opportunity.Infrastructure.Extensions;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.B2B.Seed.Contracts;
using Concertable.B2B.Seed.Infrastructure;
using Concertable.Seed.Infrastructure;
using Concertable.Seed.Shared.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;
using Xunit.Abstractions;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Messaging.Application;
using Concertable.Messaging.Contracts;
using Concertable.Messaging.Domain;
using Concertable.Messaging.Infrastructure.Outbox;
using Concertable.Shared.Email.Application;
using Concertable.Shared.Geocoding.Application;
using Concertable.Shared.Imaging.Application;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.B2B.DataAccess.Infrastructure;
using IDbInitializer = Concertable.DataAccess.Application.IDbInitializer;

namespace Concertable.B2B.IntegrationTests.Fixtures;

public class ApiFixture : IAsyncLifetime
{
    private SqlFixture sqlFixture = null!;
    private WebApplicationFactory<Program> factory = null!;
    private IServiceScope? scope;
    private readonly XunitOutputAccessor outputAccessor = new();

    public void AttachOutput(ITestOutputHelper output) => outputAccessor.Output = output;
    public void DetachOutput() => outputAccessor.Output = null;

    public IMockNotificationClient NotificationService { get; } = new MockNotificationClient();
    public MockStripeApiClient StripeApiClient { get; } = new MockStripeApiClient();
    public IMockEmailSender EmailSender { get; } = new MockEmailSender();
    public IMockManagerPaymentClient ManagerPaymentClient { get; }
    public MockPayoutAccountClient PayoutAccountClient { get; } = new();
    public MockEscrowClient EscrowClient { get; }
    public MockPaymentTransport PaymentTransport { get; } = new();

    public ApiFixture()
    {
        ManagerPaymentClient = new MockManagerPaymentClient(StripeApiClient);
        EscrowClient = new MockEscrowClient(StripeApiClient);
    }
    public IWebhookSimulator StripeClient { get; private set; } = null!;
    public SeedStateSnapshot SeedState { get; private set; } = null!;
    public DateTime SeedNow => factory.Services.GetRequiredService<SeedCatalog>().Now;

    public async Task InitializeAsync()
    {
        sqlFixture = new SqlFixture();
        await sqlFixture.InitializeAsync();
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Integration);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:B2BDb"] = sqlFixture.ConnectionString,
                    ["ExternalServices:UseRealStripe"] = "false",
                    ["ExternalServices:UseRealBlob"] = "false",
                    ["ExternalServices:UseRealEmail"] = "false",
                    ["Urls:Frontends:Venue"] = "https://localhost:5175",
                    ["Urls:Frontends:Artist"] = "https://localhost:5176",
                    ["BlobStorage:ContainerName"] = "images",
                });
                config.RelaxRateLimiting(RateLimitPolicies.All);
            });

            builder.ConfigureTestServices(services =>
            {
                services.AddXunitLogging(outputAccessor);
                services.RemoveAzureServiceBus();
                services.AddTransient<IStartupFilter, TestClientIpStartupFilter>();

                services.AddSingleton(PaymentTransport);
                services.Replace(ServiceDescriptor.Singleton<IBusTransport>(PaymentTransport));
                services.AddSingleton<INotificationClient>(NotificationService);
                services.AddSingleton(StripeApiClient);
                services.AddResettables(NotificationService, StripeApiClient, EmailSender, ManagerPaymentClient, PayoutAccountClient, EscrowClient, PaymentTransport);
                services.AddSingleton<IEmailTransport>(EmailSender);

                services.AddSingleton<IManagerPaymentOperationsClient>(ManagerPaymentClient);
                services.AddSingleton<IManagerPaymentReportingClient>(ManagerPaymentClient);
                services.AddSingleton<IEscrowOperationsClient>(EscrowClient);
                services.AddSingleton<IPayoutAccountOperationsClient>(PayoutAccountClient);

                services.AddSingleton<IWebhookSimulator, MockWebhookSimulator>();
                services.Replace(ServiceDescriptor.Singleton<IHttpClientFactory>(_ => new WebApplicationHttpClientFactory(factory)));
                services.AddScoped<IGeocodingClient, MockGeocodingClient>();
                services.AddScoped<IImageService, MockImageService>();
                services.AddScoped<IDbInitializer, IntegrationDbInitializer>();
                services.AddSeedingInfrastructure();
                services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatchInterceptor, SeedingDomainEventDispatchInterceptor>());
                services.AddSingleton<SeedCatalog>();
                services.AddScoped<SeedState>();
                services.AddUserTestSeeder();
                services.AddTenantTestSeeder();
                services.AddAdminTestSeeder();
                services.AddArtistTestSeeder();
                services.AddVenueTestSeeder();
                services.AddDealTestSeeder();
                services.AddOpportunityTestSeeder();
                services.AddApplicationTestSeeder();
                services.AddBookingTestSeeder();
                services.AddConcertTestSeeder();
                services.AddConversationsTestSeeder();

                services.AddTestAuthentication();
            });
        });

        _ = factory.Services;
        PaymentTransport.Connect(factory.Services.GetRequiredService<IServiceScopeFactory>());

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
        SeedState = new SeedStateSnapshot(scope.ServiceProvider.GetRequiredService<SeedState>());
        OnReset(scope);
    }

    protected virtual void OnReset(IServiceScope scope) { }

    public async Task SendEscrowFailedWebhookAsync(int bookingId)
    {
        if (PaymentTransport.Commands.Any(command => command is CaptureEscrowCommand or DepositEscrowCommand))
        {
            await PaymentTransport.RejectLatestAcceptanceAsync(factory.Services.GetRequiredService<IServiceScopeFactory>());
            return;
        }

        await SendPaymentFailedWebhookAsync(TransactionTypes.Escrow, bookingId);
    }

    public Task SendSettlementFailedWebhookAsync(int bookingId) =>
        SendPaymentFailedWebhookAsync(TransactionTypes.Settlement, bookingId);

    private Task SendPaymentFailedWebhookAsync(string transactionType, int bookingId)
    {
        var envelope = new MessageEnvelope(Guid.NewGuid(), MessageTypeAttribute.Resolve(typeof(PaymentFailedEvent)), DateTimeOffset.UtcNow);
        var evt = new PaymentFailedEvent($"pi_fail_{bookingId}", "card_declined", "Card was declined", new Dictionary<string, string>
        {
            [PaymentMetadataKeys.Type] = transactionType,
            [PaymentMetadataKeys.BookingId] = bookingId.ToString()
        });

        return factory.Services.GetRequiredService<IScoped<IEnumerable<IIntegrationEventHandler<PaymentFailedEvent>>>>()
            .RunAsync(async handlers =>
            {
                foreach (var handler in handlers)
                    await handler.HandleAsync(evt, envelope);
            });
    }

    public Task CompleteLatestFinancialOperationAsync() =>
        PaymentTransport.CompleteLatestAsync(factory.Services.GetRequiredService<IServiceScopeFactory>());

    public Task CompleteLatestFinancialOperationAsync<TCommand>()
        where TCommand : IIntegrationCommand =>
        PaymentTransport.CompleteLatestAsync<TCommand>(factory.Services.GetRequiredService<IServiceScopeFactory>());

    public Task RejectLatestFinancialOperationAsync() =>
        PaymentTransport.RejectLatestAsync(factory.Services.GetRequiredService<IServiceScopeFactory>());

    public Task DispatchIntegrationEventAsync<TEvent>(TEvent @event, MessageEnvelope envelope)
        where TEvent : IIntegrationEvent =>
        factory.Services.GetRequiredService<IScoped<IEnumerable<IIntegrationEventHandler<TEvent>>>>()
            .RunAsync(async handlers =>
            {
                foreach (var handler in handlers)
                    await handler.HandleAsync(@event, envelope);
            });

    public IServiceProvider Services => factory.Services;

    public async Task<IReadOnlyCollection<(string UserId, object Payload)>> WaitForDraftNotificationsAsync(
        int count)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);
        while (DateTimeOffset.UtcNow <= deadline)
        {
            var notifications = NotificationService.DraftCreated.ToArray();
            if (notifications.Length >= count)
                return notifications;

            await Task.Delay(100);
        }

        throw new InvalidOperationException($"Expected {count} concert draft notifications within 5 seconds.");
    }

    public async Task<IReadOnlyList<SendEmailCommand>> GetStagedEmailsAsync()
    {
        using var readScope = factory.Services.CreateScope();
        var outbox = readScope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var serializer = readScope.ServiceProvider.GetRequiredService<MessageSerializer>();
        var messageType = MessageTypeAttribute.Resolve(typeof(SendEmailCommand));

        var rows = await outbox.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(m => m.MessageType == messageType)
            .OrderBy(m => m.OccurredAtUtc)
            .ToListAsync();

        return rows
            .Select(r => (SendEmailCommand)serializer.Deserialize(BinaryData.FromString(r.Payload), typeof(SendEmailCommand)))
            .ToList();
    }

    public async Task<int> GetOutboxMessageCountAsync<TMessage>()
    {
        var messageType = MessageTypeAttribute.Resolve(typeof(TMessage));
        return await factory.Services
            .GetRequiredService<IScoped<OutboxDbContext>>()
            .RunAsync(outbox => outbox.Set<OutboxMessageEntity>()
                .AsNoTracking()
                .CountAsync(message => message.MessageType == messageType));
    }

    public Task<OutboxMessageSnapshot> GetOutboxMessageAsync(string messageType) => factory.Services
        .GetRequiredService<IScoped<OutboxDbContext>>()
        .RunAsync(async outbox =>
        {
            var row = await outbox.Set<OutboxMessageEntity>()
                .AsNoTracking()
                .SingleAsync(message => message.MessageType == messageType);
            return new OutboxMessageSnapshot(row.Id, row.Payload, row.Status == OutboxStatus.Dispatched);
        });

    public Task<OutboxMessageSnapshot> GetOutboxMessageAsync(Guid id) => factory.Services
        .GetRequiredService<IScoped<OutboxDbContext>>()
        .RunAsync(async outbox =>
        {
            var row = await outbox.Set<OutboxMessageEntity>()
                .AsNoTracking()
                .SingleAsync(message => message.Id == id);
            return new OutboxMessageSnapshot(row.Id, row.Payload, row.Status == OutboxStatus.Dispatched);
        });

    public HttpClient CreateClient(SeedUserSnapshot user) =>
        CreateClient(user.Id, user.Email);

    public HttpClient CreateClient(Guid userId, string email)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        return client;
    }

    public HttpClient CreateClient(SeedUserSnapshot user, Action<TestClientOptions> configure) =>
        CreateClient(user.Id, user.Email, configure);

    private HttpClient CreateClient(Guid userId, string email, Action<TestClientOptions> configure)
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
        client.DefaultRequestHeaders.Add(TestAuthHandler.EmailHeader, email);
        return client;
    }

    public HttpClient CreateClient() => factory.CreateClient();
}

public sealed record OutboxMessageSnapshot(Guid Id, string Payload, bool IsDispatched);
