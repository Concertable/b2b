using Concertable.B2B.Application.Contracts.Events;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Events;

internal sealed class ApplicationAcceptedIntegrationEventHandler : IIntegrationEventHandler<ApplicationAcceptedEvent>
{
    private readonly OpportunityDbContext context;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public ApplicationAcceptedIntegrationEventHandler(
        OpportunityDbContext context,
        IUnitOfWorkBehavior unitOfWorkBehavior)
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
            var opportunity = await context.Opportunities
                .SingleOrDefaultAsync(value => value.Id == @event.OpportunityId, ct);
            opportunity?.MarkFilled();
        }, ct);
}
