using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class VerifyPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IPaymentVerificationRecorder recorder;
    private readonly IBookingAdvancer bookingAdvancer;
    private readonly ConcertDbContext context;
    private readonly ILogger<VerifyPaymentProcessor> logger;

    public VerifyPaymentProcessor(
        IPaymentVerificationRecorder recorder,
        IBookingAdvancer bookingAdvancer,
        ConcertDbContext context,
        ILogger<VerifyPaymentProcessor> logger)
    {
        this.recorder = recorder;
        this.bookingAdvancer = bookingAdvancer;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Verify)
            return;

        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentProcessor), ct))
            return;

        var applicationId = @event.Metadata.GetValueAs<int>(PaymentMetadataKeys.ApplicationId);
        logger.VerifyWebhookReceived(@event.TransactionId, applicationId);

        context.AddInboxMessage(envelope, nameof(VerifyPaymentProcessor));

        try
        {
            await recorder.RecordVerifiedAsync(applicationId, ct);
            await bookingAdvancer.AdvanceIfReadyAsync(applicationId, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
