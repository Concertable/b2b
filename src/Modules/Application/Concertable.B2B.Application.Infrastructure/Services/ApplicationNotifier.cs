using Concertable.B2B.Conversations.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Notifications;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IConversationsModule conversationsModule;
    private readonly INotificationClient notificationClient;

    public ApplicationNotifier(
        IApplicationRepository repository,
        ICurrentUser currentUser,
        IConversationsModule conversationsModule,
        INotificationClient notificationClient)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.conversationsModule = conversationsModule;
        this.notificationClient = notificationClient;
    }

    public Task VerifyPaymentFailedAsync(int applicationId, string venueManagerId, string failureMessage) =>
        notificationClient.SendAsync(
            venueManagerId,
            "VerifyPaymentFailed",
            new { applicationId, failureMessage });

    public Task AppliedAsync(int applicationId) =>
        NotifyVenueAsync(
            applicationId,
            $"{currentUser.Email} has applied to your concert opportunity",
            MessageAction.ApplicationReceived);

    public Task WithdrawnAsync(int applicationId) =>
        NotifyVenueAsync(
            applicationId,
            $"{currentUser.Email} has withdrawn their application to your concert opportunity",
            MessageAction.ApplicationWithdrawn);

    public Task AcceptedAsync(int applicationId) =>
        NotifyArtistAsync(
            applicationId,
            "Your application has been accepted!",
            MessageAction.ApplicationAccepted);

    public Task RejectedAsync(int applicationId) =>
        NotifyArtistAsync(
            applicationId,
            "Your application was not selected for this concert opportunity",
            MessageAction.ApplicationRejected);

    public Task CancelledAsync(int applicationId) =>
        NotifyArtistAsync(
            applicationId,
            "Your application was cancelled by the venue",
            MessageAction.ApplicationCancelled);

    private async Task NotifyVenueAsync(
        int applicationId,
        string content,
        MessageAction action)
    {
        var (venueTenantId, artistTenantId) = await repository
            .GetTenantPairByIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);

        await conversationsModule.SendAsync(
            venueTenantId,
            artistTenantId,
            artistTenantId,
            currentUser.GetId(),
            content,
            action);
    }

    private async Task NotifyArtistAsync(
        int applicationId,
        string content,
        MessageAction action)
    {
        var (venueTenantId, artistTenantId) = await repository
            .GetTenantPairByIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);

        await conversationsModule.SendAndNotifyAsync(
            venueTenantId,
            artistTenantId,
            venueTenantId,
            currentUser.GetId(),
            content,
            action);
    }
}
