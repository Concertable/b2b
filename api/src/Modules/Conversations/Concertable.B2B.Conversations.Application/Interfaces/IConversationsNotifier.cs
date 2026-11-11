namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IConversationsNotifier
{
    Task ConversationChangedAsync(Guid userId, int conversationId, CancellationToken ct = default);
}
