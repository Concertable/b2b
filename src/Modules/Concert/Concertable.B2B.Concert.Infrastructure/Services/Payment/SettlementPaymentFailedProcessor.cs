using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly ConcertDbContext context;
    private readonly ISettlementService settlementService;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;
    private readonly ILogger<SettlementPaymentFailedProcessor> logger;

    public SettlementPaymentFailedProcessor(
        ConcertDbContext context,
        ISettlementService settlementService,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior,
        ILogger<SettlementPaymentFailedProcessor> logger)
    {
        this.context = context;
        this.settlementService = settlementService;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Reference.OperationType != PaymentOperationReferences.SettlementType)
            return;
        var concertId = PaymentOperationReferences.ReadConcertId(@event.Reference);
        var operationId = @event.Metadata.GetValueAs<Guid>(PaymentMetadataKeys.OperationId);
        logger.SettlementPaymentFailed(concertId, @event.FailureCode, @event.FailureMessage);
        await settlementService.RecordFailureAsync(
            concertId,
            operationId,
            @event.FailureCode ?? "unknown",
            @event.FailureMessage ?? "Settlement payment failed.",
            ct);

        try
        {
            await outboxUnitOfWorkBehavior.ExecuteAsync(async () =>
            {
                if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentFailedProcessor), ct))
                    return;

                context.AddInboxMessage(envelope, nameof(SettlementPaymentFailedProcessor));
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
