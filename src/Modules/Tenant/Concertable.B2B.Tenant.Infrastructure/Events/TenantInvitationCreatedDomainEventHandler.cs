using Concertable.B2B.Infrastructure.Uris;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Tenant.Infrastructure.Events;

/// <summary>
/// Stages the org-invitation email on the transactional outbox when an invitation is created, so the send
/// commits with the invitation row and is retried by the outbox — instead of firing synchronously after the
/// save, where a transient failure silently lost the mail. Pre-commit so the <c>SendEmailCommand</c> row
/// enlists in the same transaction.
/// </summary>
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
