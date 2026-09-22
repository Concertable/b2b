using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
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
    private readonly IApplicationReadDbContext readDbContext;
    private readonly ITenantScope tenantScope;
    private readonly IPaymentSessionOperationsClient paymentSessions;
    private readonly ApplicationDbContext context;
    private readonly IUnitOfWork unitOfWork;
    private readonly ILogger<VerifyPaymentProcessor> logger;

    public VerifyPaymentProcessor(
        IPaymentVerificationRecorder paymentVerificationRecorder,
        IApplicationReadDbContext readDbContext,
        ITenantScope tenantScope,
        IPaymentSessionOperationsClient paymentSessions,
        ApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<VerifyPaymentProcessor> logger)
    {
        this.paymentVerificationRecorder = paymentVerificationRecorder;
        this.readDbContext = readDbContext;
        this.tenantScope = tenantScope;
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

        var venueTenantId = await readDbContext.Applications
            .Where(application => application.Id == applicationId)
            .Select(application => (Guid?)application.VenueTenantId)
            .SingleOrDefaultAsync(ct);
        var owned = venueTenantId is not null
            && (await paymentSessions.ValidatePaymentMethodAsync(
                new PaymentMethodValidationRequest(@event.Reference, venueTenantId.Value), ct)).IsSuccess;

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
            using var acting = tenantScope.As(venueTenantId!.Value);
            await paymentVerificationRecorder.RecordAsync(new VerifyPaymentSucceeded(applicationId), ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
