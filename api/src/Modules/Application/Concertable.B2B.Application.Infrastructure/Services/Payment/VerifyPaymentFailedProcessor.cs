using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Contracts.Commands;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Client;
using Concertable.Payment.Contracts;
using Concertable.Payment.Contracts.Errors;
using Concertable.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Application.Infrastructure.Services.Payment;

internal sealed class VerifyPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private const string DefaultFailureCode = "payment_failed";
    private const string DefaultFailureMessage = "Payment verification failed.";

    private readonly IPaymentVerificationRecorder paymentVerificationRecorder;
    private readonly IBus bus;
    private readonly IApplicationReadDbContext readDbContext;
    private readonly IPaymentSessionOperationsClient paymentSessions;
    private readonly ApplicationPrivilegedDbContext context;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior unitOfWork;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(
        IPaymentVerificationRecorder paymentVerificationRecorder,
        IBus bus,
        IApplicationReadDbContext readDbContext,
        IPaymentSessionOperationsClient paymentSessions,
        ApplicationPrivilegedDbContext context,
        IPrivilegedOutboxUnitOfWorkBehavior unitOfWork,
        ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.paymentVerificationRecorder = paymentVerificationRecorder;
        this.bus = bus;
        this.readDbContext = readDbContext;
        this.paymentSessions = paymentSessions;
        this.context = context;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task HandleAsync(
        PaymentFailedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        if (@event.Reference.OperationType != PaymentOperationReferences.MethodVerificationType
            || !@event.Reference.TryGetApplicationId(out var applicationId)
            || !@event.Metadata.TryGetOperationId(out var operationId))
            return;
        if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(VerifyPaymentFailedProcessor), ct))
            return;

        var venueTenantId = await readDbContext.Applications
            .Where(application => application.Id == applicationId)
            .Select(application => (Guid?)application.VenueTenantId)
            .SingleOrDefaultAsync(ct);
        var owned = false;
        if (venueTenantId is not null)
        {
            var status = await paymentSessions.GetStatusAsync(
                new PaymentSessionStatusRequest(operationId, venueTenantId.Value), ct);
            if (status.TryGetError(out var error)
                && error is PaymentOperationError.ProviderUnavailable)
            {
                throw new InvalidOperationException("Payment was unavailable while validating a verification failure.");
            }

            owned = status.IsSuccess;
        }

        try
        {
            await unitOfWork.ExecuteAsync(async () =>
            {
                context.AddInboxMessage(envelope, nameof(VerifyPaymentFailedProcessor));
                if (!owned)
                {
                    logger.VerifyOutcomeNotOwnedByVenue(@event.Reference.ClientReference, applicationId);
                    return;
                }

                var code = string.IsNullOrWhiteSpace(@event.FailureCode)
                    ? DefaultFailureCode
                    : @event.FailureCode;
                var message = string.IsNullOrWhiteSpace(@event.FailureMessage)
                    ? DefaultFailureMessage
                    : @event.FailureMessage;
                logger.VerifyPaymentFailed(applicationId, code, message);
                await paymentVerificationRecorder.RecordAsync(
                    new VerifyPaymentFailed(applicationId, new VerifyPaymentError(code, message)),
                    ct);
                await bus.SendAsync(new NotifyPaymentVerificationFailedCommand(applicationId, message), ct);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
            return;
        }
    }
}
