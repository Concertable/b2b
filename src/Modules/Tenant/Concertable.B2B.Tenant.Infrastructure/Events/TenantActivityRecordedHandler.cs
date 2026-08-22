using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

internal sealed class TenantActivityRecordedHandler : IIntegrationEventHandler<TenantActivityRecordedEvent>
{
    private readonly TenantDbContext context;
    private readonly ITenantActivityService activityService;

    public TenantActivityRecordedHandler(
        TenantDbContext context,
        ITenantActivityService activityService)
    {
        this.context = context;
        this.activityService = activityService;
    }

    public async Task HandleAsync(
        TenantActivityRecordedEvent e,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        if (await context.IsInboxMessageProcessedAsync(
                envelope.MessageId,
                nameof(TenantActivityRecordedHandler),
                ct))
            return;

        context.AddInboxMessage(envelope, nameof(TenantActivityRecordedHandler));
        await activityService.AddAsync(e.Activity, ct);
        await context.SaveChangesAsync(ct);
    }
}
