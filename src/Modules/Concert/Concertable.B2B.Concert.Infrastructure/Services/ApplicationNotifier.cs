using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.User.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationRepository repository;
    private readonly IUserModule userModule;
    private readonly ICurrentUser currentUser;
    private readonly IMessenger messenger;

    public ApplicationNotifier(
        IApplicationRepository repository,
        IUserModule userModule,
        ICurrentUser currentUser,
        IMessenger messenger)
    {
        this.repository = repository;
        this.userModule = userModule;
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

    private async Task NotifyVenueAsync(int applicationId, string content, MessageAction action, string emailSubject)
    {
        var venueManagerId = await repository.GetVenueManagerIdAsync(applicationId)
            .OrNotFound("Concert application");
        var venueManager = await userModule.GetManagerByIdAsync(venueManagerId)
            ?? throw new NotFoundException("Venue manager not found for application");

        await messenger.SendAsync(currentUser.GetId(), venueManager.Id, content, action,
            new EmailCopy(venueManager.Email!, emailSubject, content));
    }

    private async Task NotifyArtistAsync(int applicationId, string content, MessageAction action, string emailSubject, string emailBody)
    {
        var (artist, venue) = await repository.GetArtistAndVenueByIdAsync(applicationId)
            .OrNotFound("Concert application");

        await messenger.SendAndNotifyAsync(venue.UserId, artist.UserId, content, action,
            new EmailCopy(artist.Email!, emailSubject, emailBody));
    }
}
