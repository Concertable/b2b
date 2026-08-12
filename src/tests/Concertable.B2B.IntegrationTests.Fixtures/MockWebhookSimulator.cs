using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal sealed class MockWebhookSimulator : IWebhookSimulator
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly MockPaymentTransport paymentTransport;
    private readonly IServiceScopeFactory scopeFactory;

    public MockWebhookSimulator(
        MockStripeApiClient stripeApiClient,
        MockPaymentTransport paymentTransport,
        IServiceScopeFactory scopeFactory)
    {
        this.stripeApiClient = stripeApiClient;
        this.paymentTransport = paymentTransport;
        this.scopeFactory = scopeFactory;
    }

    public async Task SendWebhookAsync()
    {
        if (string.IsNullOrEmpty(stripeApiClient.LastPaymentIntentId))
            throw new InvalidOperationException("No payment intent from the last checkout; cannot simulate webhook.");

        if (stripeApiClient.LastMetadata.GetValueOrDefault(PaymentMetadataKeys.Type) == TransactionTypes.Escrow)
        {
            if (paymentTransport.Commands.Count == 0 || paymentTransport.HasPendingCommand)
                await paymentTransport.CompleteLatestAcceptanceAsync(scopeFactory);
            return;
        }

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
