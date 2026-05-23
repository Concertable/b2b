namespace Concertable.B2B.Conversations.Contracts;

public interface IConversationsModule
{
    Task SendAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null);
    Task SendAndNotifyAsync(Guid fromUserId, Guid toUserId, string content, MessageAction? action = null);
}
