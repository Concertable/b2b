using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.Messaging.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Handlers;

internal sealed class TenantDisplayChangedHandler : IIntegrationEventHandler<TenantDisplayChanged>
{
    private readonly ConversationsPrivilegedDbContext context;

    public TenantDisplayChangedHandler(ConversationsPrivilegedDbContext context)
    {
        this.context = context;
    }

    public async Task HandleAsync(
        TenantDisplayChanged e,
        MessageEnvelope envelope,
        CancellationToken ct = default)
    {
        var display = await context.TenantDisplays.SingleOrDefaultAsync(
            candidate => candidate.TenantId == e.TenantId,
            ct);
        if (display is null)
            context.TenantDisplays.Add(TenantDisplay.Create(e.TenantId, e.Version, e.DisplayName));
        else
            display.Apply(e.Version, e.DisplayName);
        await context.SaveChangesAsync(ct);
    }
}
