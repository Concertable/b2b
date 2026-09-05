using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Notifications;
using DisplayNames = Concertable.B2B.Application.Contracts.DisplayNames;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IConversationsModule conversationsModule;
    private readonly INotificationClient notificationClient;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;

    public ApplicationNotifier(
        IApplicationRepository repository,
        ICurrentUser currentUser,
        IConversationsModule conversationsModule,
        INotificationClient notificationClient,
        IOpportunityModule opportunityModule,
        IVenueModule venueModule)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.conversationsModule = conversationsModule;
        this.notificationClient = notificationClient;
        this.opportunityModule = opportunityModule;
        this.venueModule = venueModule;
    }

    public async Task VerifyPaymentFailedAsync(int applicationId, string failureMessage)
    {
        var application = await repository.GetByIdAsync(applicationId).OrNotFound(DisplayNames.Application);
        var opportunity = await opportunityModule.GetAsync(application.OpportunityId);
        if (!opportunity.TryGetValue(out var value))
            return;

        var venue = await venueModule.GetProfileAsync(value.VenueId);
        if (!venue.TryGetValue(out var profile))
            return;

        await notificationClient.SendAsync(
            profile.UserId.ToString(),
            "VerifyPaymentFailed",
            new { applicationId, failureMessage });
    }

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
            .GetByIdAsync(applicationId, VenueArtistTenantSpecification<ApplicationEntity>.CreatePair())
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
            .GetByIdAsync(applicationId, VenueArtistTenantSpecification<ApplicationEntity>.CreatePair())
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
