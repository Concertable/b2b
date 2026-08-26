using Concertable.B2B.Infrastructure.Uris;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

internal sealed class TenantInvitationCreatedDomainEventHandler : IPreCommitDomainEventHandler<TenantInvitationCreatedDomainEvent>
{
    private readonly IBus bus;
    private readonly IFrontendUriGenerator uris;

    public TenantInvitationCreatedDomainEventHandler(IBus bus, IFrontendUriGenerator uris)
    {
        this.bus = bus;
        this.uris = uris;
    }

    public Task HandleAsync(TenantInvitationCreatedDomainEvent e, CancellationToken ct = default)
    {
        var acceptLink = uris.Create(e.TenantType, $"/settings/members/accept/{e.InvitationId}");

        const string subject = "You've been invited to join an organization on Concertable";
        var body =
            $"You've been invited to join an organization on Concertable as {e.Role}. " +
            $"Register or sign in on the manager portal, then accept your invitation here: {acceptLink}";

        return bus.SendAsync(new SendEmailCommand(e.Email, subject, body), ct);
    }
}
