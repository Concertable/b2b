using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.DataAccess.Application;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Events;

internal sealed class ConcertAvailabilityIntegrationEventHandler :
    IIntegrationEventHandler<ConcertCreatedEvent>,
    IIntegrationEventHandler<ConcertCancelledEvent>
{
    private readonly ApplicationDbContext dbContext;
    private readonly IApplicationReadDbContext readDbContext;
    private readonly ITenantScope tenantScope;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public ConcertAvailabilityIntegrationEventHandler(
        ApplicationDbContext dbContext,
        IApplicationReadDbContext readDbContext,
        ITenantScope tenantScope,
        IUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.dbContext = dbContext;
        this.readDbContext = readDbContext;
        this.tenantScope = tenantScope;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public async Task HandleAsync(
        ConcertCreatedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        // The scope wraps the unit of work rather than sitting inside it: the save runs after the
        // block returns.
        using var acting = tenantScope.As(@event.VenueTenantId);
        await unitOfWorkBehavior.ExecuteAsync(async () =>
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
    }

    public async Task HandleAsync(
        ConcertCancelledEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        // The cancellation carries no tenant, so the owner comes off the row itself through the
        // unfiltered read stance; the removal then runs as that tenant, where the filter can see it.
        var venueTenantId = await readDbContext.ConcertAvailabilities
            .Where(value => value.ConcertId == @event.ConcertId)
            .Select(value => (Guid?)value.VenueTenantId)
            .SingleOrDefaultAsync(ct);

        using var acting = venueTenantId is null ? null : tenantScope.As(venueTenantId.Value);
        await unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = $"{nameof(ConcertAvailabilityIntegrationEventHandler)}.Cancellation";
            if (await dbContext.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            dbContext.AddInboxMessage(envelope, handler);
            if (venueTenantId is null)
                return;

            var availability = await dbContext.ConcertAvailabilities
                .SingleOrDefaultAsync(value => value.ConcertId == @event.ConcertId, ct);
            if (availability is not null)
                dbContext.ConcertAvailabilities.Remove(availability);
        }, ct);
    }
}
