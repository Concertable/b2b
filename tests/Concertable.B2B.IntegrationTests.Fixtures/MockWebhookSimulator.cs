using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal sealed class MockWebhookSimulator : IWebhookSimulator
{
    private readonly MockPaymentOperations paymentOperations;
    private readonly MockPaymentTransport paymentTransport;
    private readonly IServiceScopeFactory scopeFactory;

    public MockWebhookSimulator(
        MockPaymentOperations paymentOperations,
        MockPaymentTransport paymentTransport,
        IServiceScopeFactory scopeFactory)
    {
        this.paymentOperations = paymentOperations;
        this.paymentTransport = paymentTransport;
        this.scopeFactory = scopeFactory;
    }

    // An escrow command settles over the bus; everything B2B opened directly settles by an outcome event
    // for its reference. The acceptance command owns both branches while it is in play -- a flow that also
    // opened a checkout session must redeliver the acceptance outcome rather than re-announce the session,
    // which no handler consumes -- and every envelope id is stable per operation, so a repeat is a genuine
    // redelivery in either branch.
    public async Task SendWebhookAsync()
    {
        if (await paymentTransport.WaitForPendingAcceptanceAsync(TimeSpan.FromSeconds(2)))
        {
            await paymentTransport.CompleteLatestAcceptanceAsync(scopeFactory);
            return;
        }

        if (paymentTransport.HasSettledAcceptance)
        {
            await paymentTransport.RedeliverLatestAcceptanceAsync(scopeFactory);
            return;
        }

        if (paymentOperations.Latest is { } operation)
        {
            await DispatchAsync(operation);
            return;
        }

        await paymentTransport.CompleteLatestAcceptanceAsync(scopeFactory);
    }

    private async Task DispatchAsync(MockPaymentOperation operation)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<PaymentSucceededEvent>>();
        var envelope = new MessageEnvelope(
            PaymentOperationEnvelopes.StableId(operation.Reference),
            MessageTypeAttribute.Resolve(typeof(PaymentSucceededEvent)),
            DateTimeOffset.UtcNow);
        var @event = new PaymentSucceededEvent(
            operation.Reference,
            PaymentOperationEnvelopes.Metadata(operation.Reference, operation.OperationId));

        foreach (var handler in handlers)
            await handler.HandleAsync(@event, envelope, CancellationToken.None);
    }
}
