using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class EscrowPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly IEscrowExecutor escrowExecutor;
    private readonly IBookingRepository bookingRepository;
    private readonly ConcertTenantDbContext context;
    private readonly ILogger<EscrowPaymentFailedProcessor> logger;

    public EscrowPaymentFailedProcessor(
        IEscrowExecutor escrowExecutor,
        IBookingRepository bookingRepository,
        ConcertTenantDbContext context,
        ILogger<EscrowPaymentFailedProcessor> logger)
    {
        this.escrowExecutor = escrowExecutor;
        this.bookingRepository = bookingRepository;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Escrow)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(EscrowPaymentFailedProcessor), ct))
            return;

        var bookingId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.BookingId);
        logger.BookingPaymentFailed(bookingId, @event.FailureCode, @event.FailureMessage);

        context.AddInboxMessage(envelope, nameof(EscrowPaymentFailedProcessor));

        try
        {
            var applicationId = await bookingRepository.GetApplicationIdByIdAsync(bookingId, ct);
            if (applicationId is null)
            {
                logger.BookingNotFoundForEscrowPayment(bookingId);
                await context.SaveChangesAsync(ct);
                return;
            }

            await escrowExecutor.FailedAsync(applicationId.Value, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
