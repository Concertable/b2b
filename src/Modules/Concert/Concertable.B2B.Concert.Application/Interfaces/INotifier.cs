using Concertable.B2B.Conversations.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal sealed record EmailCopy(string To, string Subject, string Body);

/// <summary>Sends a conversation message with an email copy to the recipient.
/// Verbs mirror <see cref="IConversationsModule"/>: SendAndNotify also raises an in-app notification.</summary>
internal interface INotifier
{
    Task SendAsync(Guid fromUserId, Guid toUserId, string content, MessageAction action, EmailCopy email);
    Task SendAndNotifyAsync(Guid fromUserId, Guid toUserId, string content, MessageAction action, EmailCopy email);
}
