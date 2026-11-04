using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Infrastructure.Payments;
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
    private readonly IApplicationPrivilegedRepository applicationRepository;
    private readonly IPaymentSessionOperationsClient paymentSessions;
    private readonly ApplicationPrivilegedDbContext context;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWork;
    private readonly ILogger<VerifyPaymentProcessor> logger;

    public VerifyPaymentProcessor(
        IPaymentVerificationRecorder paymentVerificationRecorder,
        IApplicationPrivilegedRepository applicationRepository,
        IPaymentSessionOperationsClient paymentSessions,
        ApplicationPrivilegedDbContext context,
        IPrivilegedUnitOfWorkBehavior unitOfWork,
        ILogger<VerifyPaymentProcessor> logger)
    {
        this.paymentVerificationRecorder = paymentVerificationRecorder;
        this.applicationRepository = applicationRepository;
        this.paymentSessions = paymentSessions;
        this.context = context;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task HandleAsync(
        PaymentSucceededEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        if (@event.Reference.OperationType != PaymentOperationReferences.MethodVerificationType
            || !@event.Reference.TryGetApplicationId(out var applicationId))
            return;
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentProcessor), ct))
            return;

        var venueTenantId = await applicationRepository.GetVenueTenantIdAsync(applicationId, ct);
        var owned = venueTenantId is { } payerOwnerId
            && (await paymentSessions.ValidatePaymentMethodAsync(
                new PaymentMethodValidationRequest(@event.Reference, payerOwnerId), ct)).IsSuccess;

        try
        {
            await unitOfWork.ExecuteAsync(async () =>
            {
                context.AddInboxMessage(envelope, nameof(VerifyPaymentProcessor));
                if (!owned)
                {
                    logger.VerifyOutcomeNotOwnedByVenue(@event.Reference.ClientReference, applicationId);
                    return;
                }

                logger.VerifyWebhookReceived(@event.Reference.ClientReference, applicationId);
                await paymentVerificationRecorder.RecordAsync(new VerifyPaymentSucceeded(applicationId), ct);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
