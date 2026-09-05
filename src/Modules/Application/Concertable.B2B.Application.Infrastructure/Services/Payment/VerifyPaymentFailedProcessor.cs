using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class VerifyPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private const string DefaultFailureCode = "payment_failed";
    private const string DefaultFailureMessage = "Payment verification failed.";

    private readonly IPaymentVerificationRecorder paymentVerificationRecorder;
    private readonly IApplicationNotifier applicationNotifier;
    private readonly ApplicationDbContext context;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(
        IPaymentVerificationRecorder paymentVerificationRecorder,
        IApplicationNotifier applicationNotifier,
        ApplicationDbContext context,
        ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.paymentVerificationRecorder = paymentVerificationRecorder;
        this.applicationNotifier = applicationNotifier;
        this.context = context;
        this.logger = logger;
    }

    public async Task HandleAsync(
        PaymentFailedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        if (@event.Reference.OperationType != PaymentOperationReferences.MethodVerificationType)
            return;
        var applicationId = PaymentOperationReferences.ReadApplicationId(@event.Reference);
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentFailedProcessor), ct))
            return;

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
            await paymentVerificationRecorder.RecordAsync(
                new VerifyPaymentFailed(applicationId, new VerifyPaymentError(code, message)),
                ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
            return;
        }

        await applicationNotifier.VerifyPaymentFailedAsync(applicationId, message);
    }
}
