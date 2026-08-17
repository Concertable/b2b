using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class VerifyPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private const string DefaultFailureCode = "payment_failed";
    private const string DefaultFailureMessage = "Payment verification failed.";

    private readonly IPaymentVerificationRecorder recorder;
    private readonly ApplicationDbContext context;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(
        IPaymentVerificationRecorder recorder,
        ApplicationDbContext context,
        ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.recorder = recorder;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(
        PaymentFailedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Verify)
            return;
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentFailedProcessor), ct))
            return;

        var applicationId = int.Parse(@event.Metadata[PaymentMetadataKeys.ApplicationId]);
        var code = string.IsNullOrWhiteSpace(@event.FailureCode)
            ? DefaultFailureCode
            : @event.FailureCode;
        var message = string.IsNullOrWhiteSpace(@event.FailureMessage)
            ? DefaultFailureMessage
            : @event.FailureMessage;
        logger.VerifyPaymentFailed(applicationId, code, message);
        context.AddInboxMessage(envelope, nameof(VerifyPaymentFailedProcessor));

        try
        {
            await recorder.RecordAsync(
                new VerifyPaymentFailed(
                    applicationId,
                    @event.TransactionId,
                    new VerifyPaymentError(code, message)),
                ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
