using Concertable.Messaging.Contracts;
using Concertable.Payment.Domain.Events;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal class MockWebhookSimulatorFail : IWebhookSimulator
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly IServiceScopeFactory scopeFactory;

    public MockWebhookSimulatorFail(MockStripeApiClient stripeApiClient, IServiceScopeFactory scopeFactory)
    {
        this.stripeApiClient = stripeApiClient;
        this.scopeFactory = scopeFactory;
    }

    public async Task SendWebhookAsync()
    {
        if (string.IsNullOrEmpty(stripeApiClient.LastPaymentIntentId))
            throw new InvalidOperationException("No payment intent from the last accept; cannot simulate webhook.");

        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<PaymentFailedEvent>>();
        var envelope = new MessageEnvelope(Guid.NewGuid(), MessageTypeAttribute.Resolve(typeof(PaymentFailedEvent)), DateTimeOffset.UtcNow);
        var evt = new PaymentFailedEvent(stripeApiClient.LastPaymentIntentId, "card_declined", "Your card was declined.", stripeApiClient.LastMetadata);

        foreach (var handler in handlers)
            await handler.HandleAsync(evt, envelope, CancellationToken.None);
    }
}
