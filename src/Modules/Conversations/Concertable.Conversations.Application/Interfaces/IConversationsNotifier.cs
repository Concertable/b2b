namespace Concertable.Conversations.Application.Interfaces;

internal interface IConversationsNotifier
{
    Task MessageReceivedAsync(string userId, object payload);
}
