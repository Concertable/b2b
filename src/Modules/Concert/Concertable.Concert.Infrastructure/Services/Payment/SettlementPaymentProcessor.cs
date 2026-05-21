using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Payment;

internal class SettlementPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IConcertWorkflowModule concertWorkflowModule;
    private readonly ILogger<SettlementPaymentProcessor> logger;

    public SettlementPaymentProcessor(IConcertWorkflowModule concertWorkflowModule, ILogger<SettlementPaymentProcessor> logger)
    {
        this.concertWorkflowModule = concertWorkflowModule;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Settlement)
            return;

        var bookingId = int.Parse(@event.Metadata["bookingId"]);
        logger.LogDebug(
            "Settlement webhook received: payment intent {TransactionId} for booking {BookingId}",
            @event.TransactionId, bookingId);
        await concertWorkflowModule.SettleAsync(bookingId, ct);
    }
}
