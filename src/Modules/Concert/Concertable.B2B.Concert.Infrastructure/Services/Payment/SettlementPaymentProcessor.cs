using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Messaging.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Payment;

internal sealed class SettlementPaymentProcessor : IIntegrationEventHandler<PaymentSucceededEvent>
{
    private readonly ConcertDbContext context;
    private readonly ISettlementService settlementService;
    private readonly IOutboxUnitOfWorkBehavior outboxBehavior;
    private readonly ILogger<SettlementPaymentProcessor> logger;
    private readonly IBus bus;

    public SettlementPaymentProcessor(
        ConcertDbContext context,
        ISettlementService settlementService,
        IOutboxUnitOfWorkBehavior outboxBehavior,
        ILogger<SettlementPaymentProcessor> logger,
        IBus bus)
    {
        this.context = context;
        this.settlementService = settlementService;
        this.outboxBehavior = outboxBehavior;
        this.logger = logger;
        this.bus = bus;
    }

    public async Task HandleAsync(PaymentSucceededEvent @event, MessageEnvelope envelope, CancellationToken ct = default)
    {
        if (@event.Reference.OperationType != PaymentOperationReferences.SettlementType)
            return;
        var concertId = PaymentOperationReferences.ReadConcertId(@event.Reference);
        var operationId = @event.Metadata.GetValueAs<Guid>(PaymentMetadataKeys.OperationId);
        logger.SettlementWebhookReceived(@event.Reference.ClientReference, concertId);
        var concert = await context.Concerts
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.Id == concertId, ct)
            ?? throw new InvalidOperationException($"Settlement concert {concertId} was not found.");
        var completion = await settlementService.CompleteAsync(concert.Id, operationId, ct);
        if (completion.TryGetError(out var error))
            throw new InvalidOperationException(
                $"Concert {concert.Id} could not converge settlement: {error.Definition.Message}");

        try
        {
            await outboxBehavior.ExecuteAsync(async () =>
            {
                if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, nameof(SettlementPaymentProcessor), ct))
                    return;

                context.AddInboxMessage(envelope, nameof(SettlementPaymentProcessor));
                await PublishActivityAsync(concert.VenueTenantId, "venue", concert, envelope, ct);
                await PublishActivityAsync(concert.ArtistTenantId, "artist", concert, envelope, ct);
            }, ct);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            logger.DuplicateInboxMessage(envelope.MessageId);
        }
    }

    private Task PublishActivityAsync(
        Guid tenantId,
        string persona,
        ConcertEntity concert,
        MessageEnvelope envelope,
        CancellationToken ct) =>
        bus.PublishAsync(new TenantActivityRecordedEvent(new ActivityRecord(
            $"settlement:{envelope.MessageId}",
            tenantId,
            ActivityType.ConcertSettled,
            envelope.OccurredAtUtc,
            $"\"{concert.Name}\" settled",
            null,
            $"/_{persona}/my/concerts/concert/{concert.Id}")), ct);
}
