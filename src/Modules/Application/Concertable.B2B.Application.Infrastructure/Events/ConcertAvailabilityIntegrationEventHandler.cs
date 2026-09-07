using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Events;

internal sealed class ConcertAvailabilityIntegrationEventHandler :
    IIntegrationEventHandler<ConcertCreatedEvent>,
    IIntegrationEventHandler<ConcertCancelledEvent>
{
    private readonly ApplicationDbContext dbContext;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public ConcertAvailabilityIntegrationEventHandler(
        ApplicationDbContext dbContext,
        IUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.dbContext = dbContext;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task HandleAsync(
        ConcertCreatedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(ConcertAvailabilityIntegrationEventHandler);
            if (await dbContext.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            dbContext.AddInboxMessage(envelope, handler);
            if (!await dbContext.ConcertAvailabilities.AnyAsync(
                    availability => availability.ConcertId == @event.ConcertId,
                    ct))
                dbContext.ConcertAvailabilities.Add(ConcertAvailabilityEntity.Create(
                    @event.ConcertId,
                    @event.OpportunityId,
                    @event.ArtistId,
                    @event.VenueId,
                    @event.VenueTenantId,
                    @event.ArtistTenantId,
                    @event.StartDate));
        }, ct);

    public Task HandleAsync(
        ConcertCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = $"{nameof(ConcertAvailabilityIntegrationEventHandler)}.Cancellation";
            if (await dbContext.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            dbContext.AddInboxMessage(envelope, handler);
            var availability = await dbContext.ConcertAvailabilities
                .SingleOrDefaultAsync(value => value.ConcertId == @event.ConcertId, ct);
            if (availability is not null)
                dbContext.ConcertAvailabilities.Remove(availability);
        }, ct);
}
