using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentFailedProcessor : IIntegrationEventHandler<PaymentFailedEvent>
{
    private readonly ConcertDbContext context;
    private readonly IConcertReadDbContext readDbContext;
    private readonly ITenantScope tenantScope;
    private readonly ISettlementService settlementService;
    private readonly IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior;
    private readonly ILogger<SettlementPaymentFailedProcessor> logger;

    public SettlementPaymentFailedProcessor(
        ConcertDbContext context,
        IConcertReadDbContext readDbContext,
        ITenantScope tenantScope,
        ISettlementService settlementService,
        IOutboxUnitOfWorkBehavior outboxUnitOfWorkBehavior,
        ILogger<SettlementPaymentFailedProcessor> logger)
    {
        this.context = context;
        this.readDbContext = readDbContext;
        this.tenantScope = tenantScope;
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
        // A settlement outcome names only the concert, so the owner comes off the row itself through the
        // unfiltered read stance; the failure is then recorded as that tenant.
        var venueTenantId = await readDbContext.Concerts
            .Where(value => value.Id == concertId)
            .Select(value => (Guid?)value.VenueTenantId)
            .SingleOrDefaultAsync(ct);
        if (venueTenantId is null)
        {
            logger.SettlementOutcomeForUnknownConcert(concertId);
            await RecordInboxAsync(envelope, ct);
            return;
        }

        using var acting = tenantScope.As(venueTenantId.Value);
        await settlementService.RecordFailureAsync(
            concertId,
            operationId,
            @event.FailureCode ?? "unknown",
            @event.FailureMessage ?? "Settlement payment failed.",
            ct);
        await RecordInboxAsync(envelope, ct);
    }

    private async Task RecordInboxAsync(MessageEnvelope envelope, CancellationToken ct)
    {
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
