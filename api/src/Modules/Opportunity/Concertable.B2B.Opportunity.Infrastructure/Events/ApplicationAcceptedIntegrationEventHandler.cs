using Concertable.B2B.Application.Contracts.Events;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.DataAccess.Infrastructure.Locking;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Events;

internal sealed class ApplicationAcceptedIntegrationEventHandler : IIntegrationEventHandler<ApplicationAcceptedEvent>
{
    private readonly OpportunityPrivilegedDbContext context;
    private readonly IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior;

    public ApplicationAcceptedIntegrationEventHandler(
        OpportunityPrivilegedDbContext context,
        IPrivilegedUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.context = context;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public Task HandleAsync(
        ApplicationAcceptedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(ApplicationAcceptedIntegrationEventHandler);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            await context.Opportunities.LockRowsAsync(
                value => value.Id == @event.OpportunityId, RowLock.NoKeyUpdate, ct);
            var opportunity = await context.Opportunities
                .SingleOrDefaultAsync(
                    value => value.Id == @event.OpportunityId
                             && value.TenantId == @event.VenueTenantId,
                    ct);
            opportunity?.MarkFilled(@event.ApplicationId);
        }, ct);
}
