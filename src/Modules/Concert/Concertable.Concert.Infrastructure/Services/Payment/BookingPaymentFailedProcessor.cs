using Microsoft.Extensions.Logging;

namespace Concertable.Concert.Infrastructure.Services.Payment;

internal class BookingPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IBookingService bookingService;
    private readonly ILogger<BookingPaymentFailedProcessor> logger;

    public BookingPaymentFailedProcessor(IBookingService bookingService, ILogger<BookingPaymentFailedProcessor> logger)
    {
        this.bookingService = bookingService;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        var type = @event.Metadata.GetValueOrDefault("type");
        if (type != TransactionTypes.Settlement && type != TransactionTypes.Escrow)
            return;

        var bookingId = int.Parse(@event.Metadata["bookingId"]);
        logger.LogWarning(
            "Payment failed for booking {BookingId}: [{FailureCode}] {FailureMessage}",
            bookingId, @event.FailureCode, @event.FailureMessage);
        await bookingService.FailPaymentAsync(bookingId, ct);
    }
}
