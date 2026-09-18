using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Notifications;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationPrivilegedRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IConversationsModule conversationsModule;
    private readonly INotificationClient notificationClient;
    private readonly IOpportunityCommandFacts opportunityFacts;
    private readonly IVenueCommandFacts venueFacts;

    public ApplicationNotifier(
        IApplicationPrivilegedRepository repository,
        ICurrentUser currentUser,
        IConversationsModule conversationsModule,
        INotificationClient notificationClient,
        IOpportunityCommandFacts opportunityFacts,
        IVenueCommandFacts venueFacts)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.conversationsModule = conversationsModule;
        this.notificationClient = notificationClient;
        this.opportunityFacts = opportunityFacts;
        this.venueFacts = venueFacts;
    }

    public async Task VerifyPaymentFailedAsync(int applicationId, string failureMessage)
    {
        var opportunityId = await repository.GetOpportunityIdAsync(applicationId);
        if (opportunityId is null)
            return;
        var opportunity = await opportunityFacts.GetByIdAsync(opportunityId.Value);
        if (opportunity is null)
            return;
        var venue = await venueFacts.GetByIdAsync(opportunity.VenueId);
        if (venue is null)
            return;

        await notificationClient.SendAsync(
            venue.UserId.ToString(),
            "VerifyPaymentFailed",
            new { applicationId, failureMessage });
    }

    public Task AppliedAsync(ApplicationEntity application) =>
        NotifyVenueAsync(
            application,
            $"{currentUser.Email} has applied to your concert opportunity",
            MessageAction.ApplicationReceived);

    public Task WithdrawnAsync(ApplicationEntity application) =>
        NotifyVenueAsync(
            application,
            $"{currentUser.Email} has withdrawn their application to your concert opportunity",
            MessageAction.ApplicationWithdrawn);

    public Task AcceptedAsync(ApplicationEntity application) =>
        NotifyArtistAsync(
            application,
            "Your application has been accepted!",
            MessageAction.ApplicationAccepted);

    public Task RejectedAsync(ApplicationEntity application) =>
        NotifyArtistAsync(
            application,
            "Your application was not selected for this concert opportunity",
            MessageAction.ApplicationRejected);

    public Task CancelledAsync(ApplicationEntity application) =>
        NotifyArtistAsync(
            application,
            "Your application was cancelled by the venue",
            MessageAction.ApplicationCancelled);

    private async Task NotifyVenueAsync(
        ApplicationEntity application,
        string content,
        MessageAction action)
    {
        await conversationsModule.SendAsync(
            [application.VenueTenantId, application.ArtistTenantId],
            application.ArtistTenantId,
            currentUser.GetId(),
            content,
            action);
    }

    private async Task NotifyArtistAsync(
        ApplicationEntity application,
        string content,
        MessageAction action)
    {
        await conversationsModule.SendAndNotifyAsync(
            [application.VenueTenantId, application.ArtistTenantId],
            application.VenueTenantId,
            currentUser.GetId(),
            content,
            action);
    }
}
