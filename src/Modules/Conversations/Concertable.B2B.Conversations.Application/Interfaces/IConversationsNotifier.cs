namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IConversationsNotifier
{
    Task MessageReceivedAsync(string userId, object payload);
}
