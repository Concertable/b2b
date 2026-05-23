using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal class BookingPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IBookingService bookingService;
    private readonly ConcertDbContext context;
    private readonly ILogger<BookingPaymentFailedProcessor> logger;

    public BookingPaymentFailedProcessor(
        IBookingService bookingService,
        ConcertDbContext context,
        ILogger<BookingPaymentFailedProcessor> logger)
    {
        this.bookingService = bookingService;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        var type = @event.Metadata.GetValueOrDefault("type");
        if (type != TransactionTypes.Settlement && type != TransactionTypes.Escrow)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(BookingPaymentFailedProcessor), ct))
            return;

        var bookingId = int.Parse(@event.Metadata["bookingId"]);
        logger.BookingPaymentFailed(bookingId, @event.FailureCode, @event.FailureMessage);

        context.AddInboxMessage(envelope, nameof(BookingPaymentFailedProcessor));

        try
        {
            await bookingService.FailPaymentAsync(bookingId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
