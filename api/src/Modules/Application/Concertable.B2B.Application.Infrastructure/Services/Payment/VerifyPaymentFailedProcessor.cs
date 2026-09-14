using Concertable.B2B.Application.Contracts;
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
    private readonly IApplicationNotifier applicationNotifier;
    private readonly IApplicationRepository applicationRepository;
    private readonly IPaymentSessionOperationsClient paymentSessions;
    private readonly ApplicationDbContext context;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<VerifyPaymentFailedProcessor> logger;

    public VerifyPaymentFailedProcessor(
        IPaymentVerificationRecorder paymentVerificationRecorder,
        IApplicationNotifier applicationNotifier,
        IApplicationRepository applicationRepository,
        IPaymentSessionOperationsClient paymentSessions,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<VerifyPaymentFailedProcessor> logger)
    {
        this.paymentVerificationRecorder = paymentVerificationRecorder;
        this.applicationNotifier = applicationNotifier;
        this.applicationRepository = applicationRepository;
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

        var venueTenantId = await applicationRepository.GetByIdAsync(
            applicationId,
            VenueArtistTenantSpecification<ApplicationEntity>.CreateVenueTenantId(),
            ct);
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
            context.AddInboxMessage(envelope, nameof(VerifyPaymentFailedProcessor));
            if (!owned)
            {
                logger.VerifyOutcomeNotOwnedByVenue(@event.Reference.ClientReference, applicationId);
                await unitOfWork.SaveChangesAsync(ct);
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

            await applicationNotifier.VerifyPaymentFailedAsync(applicationId, message);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
