using Concertable.B2B.Conversations.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IConversationsModule conversationsModule;

    public ApplicationNotifier(
        IApplicationRepository repository,
        ICurrentUser currentUser,
        IConversationsModule conversationsModule)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.conversationsModule = conversationsModule;
    }

    public Task AppliedAsync(int applicationId) =>
        NotifyVenueAsync(applicationId,
            content: $"{currentUser.Email} has applied to your concert opportunity",
            action: MessageAction.ApplicationReceived);

    public Task WithdrawnAsync(int applicationId) =>
        NotifyVenueAsync(applicationId,
            content: $"{currentUser.Email} has withdrawn their application to your concert opportunity",
            action: MessageAction.ApplicationWithdrawn);

    public Task AcceptedAsync(int applicationId) =>
        NotifyArtistAsync(applicationId,
            content: "Your application has been accepted!",
            action: MessageAction.ApplicationAccepted);

    public Task RejectedAsync(int applicationId) =>
        NotifyArtistAsync(applicationId,
            content: "Your application was not selected for this concert opportunity",
            action: MessageAction.ApplicationRejected);

    public Task CancelledAsync(int applicationId) =>
        NotifyArtistAsync(applicationId,
            content: "Your accepted application has been cancelled",
            action: MessageAction.ApplicationCancelled);

    // Artist → Venue: the applying artist's tenant is the sender.
    private async Task NotifyVenueAsync(int applicationId, string content, MessageAction action)
    {
        var (venueTenantId, artistTenantId) = await repository.GetTenantPairByIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);

        await conversationsModule.SendAsync(venueTenantId, artistTenantId,
            senderTenantId: artistTenantId, sentByUserId: currentUser.GetId(), content, action);
    }

    // Venue → Artist: the acting venue's tenant is the sender.
    private async Task NotifyArtistAsync(int applicationId, string content, MessageAction action)
    {
        var (venueTenantId, artistTenantId) = await repository.GetTenantPairByIdAsync(applicationId)
            .OrNotFound(DisplayNames.Application);

        await conversationsModule.SendAndNotifyAsync(venueTenantId, artistTenantId,
            senderTenantId: venueTenantId, sentByUserId: currentUser.GetId(), content, action);
    }
}
