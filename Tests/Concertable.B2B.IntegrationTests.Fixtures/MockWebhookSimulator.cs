using Concertable.Messaging.Contracts;
using Concertable.Payment.Domain.Events;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal class MockWebhookSimulator : IWebhookSimulator
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly IServiceScopeFactory scopeFactory;

    public MockWebhookSimulator(MockStripeApiClient stripeApiClient, IServiceScopeFactory scopeFactory)
    {
        this.stripeApiClient = stripeApiClient;
        this.scopeFactory = scopeFactory;
    }

    public async Task SendWebhookAsync()
    {
        if (string.IsNullOrEmpty(stripeApiClient.LastPaymentIntentId))
            throw new InvalidOperationException("No payment intent from the last checkout; cannot simulate webhook.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<PaymentSucceededEvent>>();
        var messageId = StableGuid(stripeApiClient.LastPaymentIntentId);
        var envelope = new MessageEnvelope(messageId, MessageTypeAttribute.Resolve(typeof(PaymentSucceededEvent)), DateTimeOffset.UtcNow);
        var evt = new PaymentSucceededEvent(stripeApiClient.LastPaymentIntentId, stripeApiClient.LastMetadata);

        foreach (var handler in handlers)
            await handler.HandleAsync(evt, envelope, CancellationToken.None);
    }

    private static Guid StableGuid(string input)
    {
        var bytes = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(bytes);
    }
}
