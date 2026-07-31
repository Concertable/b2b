using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationRepository repository;
    private readonly IUserModule userModule;
    private readonly ITenantModule tenantModule;
    private readonly ICurrentUser currentUser;
    private readonly IMessenger messenger;

    public ApplicationNotifier(
        IApplicationRepository repository,
        IUserModule userModule,
        ITenantModule tenantModule,
        ICurrentUser currentUser,
        IMessenger messenger)
    {
        this.repository = repository;
        this.userModule = userModule;
        this.tenantModule = tenantModule;
        this.currentUser = currentUser;
        this.messenger = messenger;
    }

    public Task AppliedAsync(int applicationId) =>
        NotifyVenueAsync(applicationId,
            content: $"{currentUser.Email} has applied to your concert opportunity",
            action: MessageAction.ApplicationReceived,
            emailSubject: "Concert Application");

    public Task WithdrawnAsync(int applicationId) =>
        NotifyVenueAsync(applicationId,
            content: $"{currentUser.Email} has withdrawn their application to your concert opportunity",
            action: MessageAction.ApplicationWithdrawn,
            emailSubject: "Concert Application Withdrawn");

    public Task AcceptedAsync(int applicationId) =>
        NotifyArtistAsync(applicationId,
            content: "Your application has been accepted!",
            action: MessageAction.ApplicationAccepted,
            emailSubject: "Concert Application Accepted",
            emailBody: "Your application was accepted! A concert has been scheduled for you.");

    public Task RejectedAsync(int applicationId) =>
        NotifyArtistAsync(applicationId,
            content: "Your application was not selected for this concert opportunity",
            action: MessageAction.ApplicationRejected,
            emailSubject: "Concert Application Update",
            emailBody: "Your application was not selected for this concert opportunity.");

    public Task CancelledAsync(int applicationId) =>
        NotifyArtistAsync(applicationId,
            content: "Your accepted application has been cancelled",
            action: MessageAction.ApplicationCancelled,
            emailSubject: "Concert Application Cancelled",
            emailBody: "Your accepted application has been cancelled. Any payment made towards it has been refunded.");

    // Artist → Venue: the applying artist's tenant is the sender.
    private async Task NotifyVenueAsync(int applicationId, string content, MessageAction action, string emailSubject)
    {
        var (venueTenantId, artistTenantId) = await repository.GetTenantPairByIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);

        await messenger.SendAsync(venueTenantId, artistTenantId,
            senderTenantId: artistTenantId, sentByUserId: currentUser.GetId(), content, action,
            await BuildEmailCopyAsync(venueTenantId, emailSubject, content));
    }

    // Venue → Artist: the acting venue's tenant is the sender.
    private async Task NotifyArtistAsync(int applicationId, string content, MessageAction action, string emailSubject, string emailBody)
    {
        var (venueTenantId, artistTenantId) = await repository.GetTenantPairByIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);

        await messenger.SendAndNotifyAsync(venueTenantId, artistTenantId,
            senderTenantId: venueTenantId, sentByUserId: currentUser.GetId(), content, action,
            await BuildEmailCopyAsync(artistTenantId, emailSubject, emailBody));
    }

    private async Task<EmailCopy> BuildEmailCopyAsync(Guid recipientTenantId, string subject, string body)
    {
        var memberIds = await tenantModule.GetMemberUserIdsAsync(recipientTenantId);
        var emails = (await userModule.GetEmailsByIdsAsync(memberIds)).Values.ToList();
        return new EmailCopy(emails, subject, body);
    }
}
