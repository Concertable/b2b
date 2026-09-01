using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class VerifyPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly IPaymentVerificationRecorder paymentVerificationRecorder;
    private readonly ApplicationDbContext context;
    private readonly ILogger<VerifyPaymentProcessor> logger;

    public VerifyPaymentProcessor(
        IPaymentVerificationRecorder paymentVerificationRecorder,
        ApplicationDbContext context,
        ILogger<VerifyPaymentProcessor> logger)
    {
        this.paymentVerificationRecorder = paymentVerificationRecorder;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(
        PaymentSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        if (@event.Metadata.GetValueOrDefault(PaymentMetadataKeys.Type) != TransactionTypes.Verify)
            return;
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentProcessor), ct))
            return;

        var applicationId = int.Parse(@event.Metadata[PaymentMetadataKeys.ApplicationId]);
        logger.VerifyWebhookReceived(@event.TransactionId, applicationId);
        context.AddInboxMessage(envelope, nameof(VerifyPaymentProcessor));

        try
        {
            await paymentVerificationRecorder.RecordAsync(
                new VerifyPaymentSucceeded(applicationId, @event.TransactionId),
                ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
