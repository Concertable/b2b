namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal interface IConversationsReadDbContext
{
    IQueryable<MessageEntity> Messages { get; }
}
