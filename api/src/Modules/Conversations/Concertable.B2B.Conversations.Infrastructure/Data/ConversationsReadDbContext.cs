using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsReadDbContext(
    DbContextOptions<ConversationsReadDbContext> options,
    ConversationsConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IConversationsReadDbContext
{
    IQueryable<MessageEntity> IConversationsReadDbContext.Messages => Query<MessageEntity>();
}
