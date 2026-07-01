using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class EscrowPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IEscrowDispatcher escrowDispatcher;
    private readonly ConcertDbContext context;
    private readonly ILogger<EscrowPaymentFailedProcessor> logger;

    public EscrowPaymentFailedProcessor(
        IEscrowDispatcher escrowDispatcher,
        ConcertDbContext context,
        ILogger<EscrowPaymentFailedProcessor> logger)
    {
        this.escrowDispatcher = escrowDispatcher;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault("type") != TransactionTypes.Escrow)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(EscrowPaymentFailedProcessor), ct))
            return;

        var bookingId = int.Parse(@event.Metadata["bookingId"]);
        logger.BookingPaymentFailed(bookingId, @event.FailureCode, @event.FailureMessage);

        context.AddInboxMessage(envelope, nameof(EscrowPaymentFailedProcessor));

        try
        {
            await escrowDispatcher.FailedAsync(bookingId);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
