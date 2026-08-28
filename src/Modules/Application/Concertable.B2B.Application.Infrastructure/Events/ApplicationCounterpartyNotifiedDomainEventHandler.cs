using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Contracts;
using Concertable.Shared.Email.Application;

namespace Concertable.B2B.Application.Infrastructure.Events;

internal sealed class ApplicationCounterpartyNotifiedDomainEventHandler
    : IPreCommitDomainEventHandler<ApplicationCounterpartyNotifiedDomainEvent>
{
    private readonly ITenantModule tenantModule;
    private readonly IUserModule userModule;
    private readonly ICurrentUser currentUser;
    private readonly IBus bus;

    public ApplicationCounterpartyNotifiedDomainEventHandler(
        ITenantModule tenantModule,
        IUserModule userModule,
        ICurrentUser currentUser,
        IBus bus)
    {
        this.tenantModule = tenantModule;
        this.userModule = userModule;
        this.currentUser = currentUser;
        this.bus = bus;
    }

    public async Task HandleAsync(ApplicationCounterpartyNotifiedDomainEvent e, CancellationToken ct = default)
    {
        var (subject, body) = Copy(e.Kind);
        var memberIds = await tenantModule.GetMemberUserIdsAsync(e.RecipientTenantId, ct);
        var emails = (await userModule.GetEmailsByIdsAsync(memberIds)).Values;
        foreach (var email in emails)
            await bus.SendAsync(new SendEmailCommand(email, subject, body), ct);
    }

    private (string Subject, string Body) Copy(ApplicationNotification kind) => kind switch
    {
        ApplicationNotification.Applied =>
            ("Concert Application", $"{currentUser.Email} has applied to your concert opportunity"),
        ApplicationNotification.Withdrawn =>
            ("Concert Application Withdrawn", $"{currentUser.Email} has withdrawn their application to your concert opportunity"),
        ApplicationNotification.Accepted =>
            ("Concert Application Accepted", "Your application was accepted! A concert has been scheduled for you."),
        ApplicationNotification.Rejected =>
            ("Concert Application Update", "Your application was not selected for this concert opportunity."),
        ApplicationNotification.ConcertCancelled =>
            ("Concert Application Cancelled", "Your scheduled concert has been cancelled."),
        ApplicationNotification.Cancelled =>
            ("Concert Application Cancelled", "Your application was cancelled by the venue."),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}
