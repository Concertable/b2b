namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ConversationsNotifier : IConversationsNotifier
{
    private readonly INotificationClient notificationClient;

    public ConversationsNotifier(INotificationClient notificationClient)
    {
        this.notificationClient = notificationClient;
    }

    public Task MessageReceivedAsync(string userId, object payload) =>
        notificationClient.SendAsync(userId, "MessageReceived", payload);
}
