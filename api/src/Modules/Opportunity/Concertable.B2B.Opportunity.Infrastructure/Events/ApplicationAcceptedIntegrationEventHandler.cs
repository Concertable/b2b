using Concertable.B2B.Application.Contracts.Events;
using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Events;

internal sealed class ApplicationAcceptedIntegrationEventHandler : IIntegrationEventHandler<ApplicationAcceptedEvent>
{
    private readonly OpportunityDbContext context;
    private readonly IOpportunityReadDbContext readDbContext;
    private readonly ITenantScope tenantScope;
    private readonly IUnitOfWorkBehavior unitOfWorkBehavior;

    public ApplicationAcceptedIntegrationEventHandler(
        OpportunityDbContext context,
        IOpportunityReadDbContext readDbContext,
        ITenantScope tenantScope,
        IUnitOfWorkBehavior unitOfWorkBehavior)
    {
        this.context = context;
        this.readDbContext = readDbContext;
        this.tenantScope = tenantScope;
        this.unitOfWorkBehavior = unitOfWorkBehavior;
    }

    public async Task HandleAsync(
        ApplicationAcceptedEvent @event,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        // The event names only the opportunity, so the owner comes off the row itself through the
        // unfiltered read stance. The scope wraps the unit of work rather than sitting inside it: the
        // save the write guard inspects runs after the block returns.
        var tenantId = await readDbContext.Opportunities
            .Where(value => value.Id == @event.OpportunityId)
            .Select(value => (Guid?)value.TenantId)
            .SingleOrDefaultAsync(ct);

        using var acting = tenantId is null ? null : tenantScope.As(tenantId.Value);
        await unitOfWorkBehavior.ExecuteAsync(async () =>
        {
            var handler = nameof(ApplicationAcceptedIntegrationEventHandler);
            if (await context.IsInboxMessageProcessedAsync(envelope.MessageId, handler, ct))
                return;

            context.AddInboxMessage(envelope, handler);
            if (tenantId is null)
                return;

            var opportunity = await context.Opportunities
                .SingleOrDefaultAsync(value => value.Id == @event.OpportunityId, ct);
            opportunity?.MarkFilled();
        }, ct);
    }
}
