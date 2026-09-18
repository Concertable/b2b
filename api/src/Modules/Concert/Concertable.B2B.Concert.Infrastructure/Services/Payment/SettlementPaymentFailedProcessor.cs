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
    private readonly ConcertPrivilegedDbContext context;
    private readonly IConcertPrivilegedRepository concertRepository;
    private readonly ISettlementService settlementService;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;
    private readonly ILogger<SettlementPaymentFailedProcessor> logger;

    public SettlementPaymentFailedProcessor(
        ConcertPrivilegedDbContext context,
        IConcertPrivilegedRepository concertRepository,
        ISettlementService settlementService,
        IPrivilegedOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior,
        ILogger<SettlementPaymentFailedProcessor> logger)
    {
        this.context = context;
        this.concertRepository = concertRepository;
        this.settlementService = settlementService;
        this.outboxUnitOfWorkBehavior = outboxUnitOfWorkBehavior;
        this.logger = logger;
    }

    public async Task HandleAsync(PaymentFailedEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Reference.OperationType != PaymentOperationReferences.SettlementType
            || !@event.Reference.TryGetConcertId(out var concertId)
            || !@event.Metadata.TryGetOperationId(out var operationId))
            return;
        logger.SettlementPaymentFailed(concertId, @event.FailureCode, @event.FailureMessage);
        try
        {
            await outboxUnitOfWorkBehavior.ExecuteAsync(async () =>
            {
                if (await context.IsInboxMessageProcessedAsync(
                        envelope.MessageId,
                        nameof(SettlementPaymentFailedProcessor),
                        ct))
                    return;

                var concert = await concertRepository.GetByIdForUpdateAsync(concertId, ct);
                if (concert is null)
                {
                    logger.SettlementOutcomeForUnknownConcert(concertId);
                    throw new InvalidOperationException(
                        $"Settlement outcome names concert {concertId}, which does not exist.");
                }

                if (concert.SettlementOperationId != operationId)
                {
                    logger.SettlementOutcomeForUnknownConcert(concertId);
                    throw new InvalidOperationException(
                        $"Settlement outcome names operation {operationId}, which concert {concertId} is not running.");
                }

                await settlementService.RecordFailureAsync(
                    concertId,
                    operationId,
                    @event.FailureCode ?? "unknown",
                    @event.FailureMessage ?? "Settlement payment failed.",
                    ct);
                context.AddInboxMessage(envelope, nameof(SettlementPaymentFailedProcessor));
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }
}
