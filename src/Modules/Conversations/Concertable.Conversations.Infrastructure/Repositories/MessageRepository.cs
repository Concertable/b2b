using Concertable.Contracts;
using Concertable.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Conversations.Infrastructure.Repositories;

internal class MessageRepository : IMessageRepository
{
    private readonly ConversationsDbContext context;

    public MessageRepository(ConversationsDbContext context)
    {
        this.context = context;
    }

    public Task<IPagination<MessageEntity>> GetByUserIdAsync(Guid id, IPageParams pageParams)
    {
        var query = context.Messages
            .Where(m => m.ToUserId == id)
            .OrderByDescending(m => m.SentDate);

        return query.ToPaginationAsync(pageParams);
    }

    public Task<int> GetUnreadCountByUserIdAsync(Guid id) =>
        context.Messages.CountAsync(m => m.ToUserId == id && !m.Read);

    public async Task MarkAsReadAsync(List<int> ids)
    {
        var messages = await context.Messages
            .Where(m => ids.Contains(m.Id))
            .ToListAsync();

        foreach (var message in messages)
            message.MarkAsRead();

        await context.SaveChangesAsync();
    }

    public async Task AddAsync(MessageEntity message)
    {
        await context.Messages.AddAsync(message);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}
