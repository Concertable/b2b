namespace Concertable.Conversations.Infrastructure.Services;

internal class ConversationsNotifier : IConversationsNotifier
{
    private readonly INotificationModule notification;

    public ConversationsNotifier(INotificationModule notification)
    {
        this.notification = notification;
    }

    public Task MessageReceivedAsync(string userId, object payload) =>
        notification.SendAsync(userId, "MessageReceived", payload);
}
