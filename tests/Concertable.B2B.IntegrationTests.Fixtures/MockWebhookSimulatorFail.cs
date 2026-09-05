using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts.Events;
using Concertable.B2B.IntegrationTests.Fixtures.Mocks;
using Concertable.Testing.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.IntegrationTests.Fixtures;

internal sealed class MockWebhookSimulatorFail : IWebhookSimulator
{
    private readonly MockPaymentOperations paymentOperations;
    private readonly MockPaymentTransport paymentTransport;
    private readonly IServiceScopeFactory scopeFactory;

    public MockWebhookSimulatorFail(
        MockPaymentOperations paymentOperations,
        MockPaymentTransport paymentTransport,
        IServiceScopeFactory scopeFactory)
    {
        this.paymentOperations = paymentOperations;
        this.paymentTransport = paymentTransport;
        this.scopeFactory = scopeFactory;
    }

    public async Task SendWebhookAsync()
    {
        if (await paymentTransport.WaitForPendingCommandAsync(TimeSpan.FromSeconds(2)))
        {
            await paymentTransport.RejectLatestAcceptanceAsync(scopeFactory);
            return;
        }

        if (paymentOperations.Latest is { } operation)
        {
            await DispatchAsync(operation);
            return;
        }

        await paymentTransport.RejectLatestAcceptanceAsync(scopeFactory);
    }

    private async Task DispatchAsync(MockPaymentOperation operation)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<PaymentFailedEvent>>();
        var envelope = MessageEnvelope.Create<PaymentFailedEvent>(DateTimeOffset.UtcNow);
        var @event = new PaymentFailedEvent(
            operation.Reference,
            "card_declined",
            "Your card was declined.",
            PaymentOperationEnvelopes.Metadata(operation.Reference, operation.OperationId));

        foreach (var handler in handlers)
            await handler.HandleAsync(@event, envelope, CancellationToken.None);
    }
}
