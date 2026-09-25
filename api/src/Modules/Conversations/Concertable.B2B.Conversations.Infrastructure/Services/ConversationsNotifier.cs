namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class ConversationsNotifier : IConversationsNotifier
{
    private readonly INotificationClient notificationClient;

    public ConversationsNotifier(INotificationClient notificationClient)
    {
        this.notificationClient = notificationClient;
    }

    public Task ConversationChangedAsync(Guid userId, int conversationId, CancellationToken ct = default) =>
        notificationClient.SendAsync(
            userId.ToString(),
            "ConversationChanged",
            new { ConversationId = conversationId });
}
