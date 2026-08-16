using Concertable.B2B.User.Domain.Events;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.User.Infrastructure.Events;

internal sealed class AdminInvitationCreatedDomainEventHandler : IPreCommitDomainEventHandler<AdminInvitationCreatedDomainEvent>
{
    private readonly IBus bus;

    public AdminInvitationCreatedDomainEventHandler(IBus bus)
    {
        this.bus = bus;
    }

    public Task HandleAsync(AdminInvitationCreatedDomainEvent e, CancellationToken ct = default)
    {
        const string subject = "You've been invited to become a Concertable admin";
        var body =
            $"You've been invited to become a Concertable admin. Register or sign in to the admin " +
            $"console using this email address ({e.Email}) to accept your invitation.";

        return bus.SendAsync(new SendEmailCommand(e.Email, subject, body), ct);
    }
}
