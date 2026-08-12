using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal sealed class MockWebhookSimulatorFail : IWebhookSimulator
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly MockPaymentTransport paymentTransport;
    private readonly IServiceScopeFactory scopeFactory;

    public MockWebhookSimulatorFail(
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
        {
            if (paymentTransport.Commands.Count == 0 || paymentTransport.HasPendingCommand)
                await paymentTransport.RejectLatestAcceptanceAsync(scopeFactory);
            return;
        }

        if (stripeApiClient.LastMetadata.GetValueOrDefault(PaymentMetadataKeys.Type) is
            TransactionTypes.ApplicationAccept or TransactionTypes.ApplicationApply or TransactionTypes.Escrow)
        {
            if (paymentTransport.Commands.Count == 0 || paymentTransport.HasPendingCommand)
                await paymentTransport.RejectLatestAcceptanceAsync(scopeFactory);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<PaymentFailedEvent>>();
        var envelope = MessageEnvelope.Create<PaymentFailedEvent>(DateTimeOffset.UtcNow);
        var evt = new PaymentFailedEvent(stripeApiClient.LastPaymentIntentId, "card_declined", "Your card was declined.", stripeApiClient.LastMetadata);

        foreach (var handler in handlers)
            await handler.HandleAsync(evt, envelope, CancellationToken.None);
    }
}
