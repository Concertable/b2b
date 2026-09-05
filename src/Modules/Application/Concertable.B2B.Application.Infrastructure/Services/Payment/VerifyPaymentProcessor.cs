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
    private readonly IApplicationRepository applicationRepository;
    private readonly IPaymentSessionOperationsClient paymentSessions;
    private readonly ApplicationDbContext context;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<VerifyPaymentProcessor> logger;

    public VerifyPaymentProcessor(
        IPaymentVerificationRecorder paymentVerificationRecorder,
        IApplicationRepository applicationRepository,
        IPaymentSessionOperationsClient paymentSessions,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
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

        var venueTenantId = await applicationRepository.GetByIdAsync(
            applicationId,
            VenueArtistTenantSpecification<ApplicationEntity>.CreateVenueTenantId(),
            ct);
        var owned = venueTenantId is { } payerOwnerId
            && (await paymentSessions.ValidatePaymentMethodAsync(
                new PaymentMethodValidationRequest(@event.Reference, payerOwnerId), ct)).IsSuccess;

        context.AddInboxMessage(envelope, nameof(VerifyPaymentProcessor));
        try
        {
            if (!owned)
            {
                logger.VerifyOutcomeNotOwnedByVenue(@event.Reference.ClientReference, applicationId);
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }

            logger.VerifyWebhookReceived(@event.Reference.ClientReference, applicationId);
            await paymentVerificationRecorder.RecordAsync(new VerifyPaymentSucceeded(applicationId), ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
