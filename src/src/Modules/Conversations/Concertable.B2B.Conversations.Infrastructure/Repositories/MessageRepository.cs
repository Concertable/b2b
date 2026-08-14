using Concertable.Contracts;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessageRepository : IMessageRepository
{
    private readonly ConversationsDbContext context;

    public MessageRepository(ConversationsDbContext context)
    {
        this.context = context;
    }

    public Task<MessageEntity?> GetByIdAsync(int id) =>
        context.Messages.FirstOrDefaultAsync(m => m.Id == id);

    public Task<IPagination<MessageEntity>> GetByTenantIdAsync(Guid tenantId, IPageParams pageParams) =>
        context.Messages
            .OrderByDescending(m => m.SentDate)
            .ToPaginationAsync(pageParams);

    public Task<int> GetUnreadCountByTenantIdAsync(Guid tenantId, Guid userId) =>
        (from m in context.Messages.Where(m => m.SenderTenantId != tenantId)
         join p in context.ThreadReadStates.Where(p => p.UserId == userId)
             on new { m.VenueTenantId, m.ArtistTenantId } equals new { p.VenueTenantId, p.ArtistTenantId } into pointers
         from p in pointers.DefaultIfEmpty()
         where p == null || m.SentDate > p.LastReadAt
         select m.Id)
        .CountAsync();

    public async Task AdvanceReadPointersAsync(Guid tenantId, Guid userId, DateTime readAt)
    {
        var pairs = await context.Messages
            .Select(m => new { m.VenueTenantId, m.ArtistTenantId })
            .Distinct()
            .ToListAsync();

        var pointers = await context.ThreadReadStates
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => (p.VenueTenantId, p.ArtistTenantId));

        foreach (var pair in pairs)
        {
            if (pointers.TryGetValue((pair.VenueTenantId, pair.ArtistTenantId), out var pointer))
                pointer.Advance(readAt);
            else
                await context.ThreadReadStates.AddAsync(
                    ThreadReadStateEntity.Create(pair.VenueTenantId, pair.ArtistTenantId, userId, readAt));
        }

        await context.SaveChangesAsync();
    }

    public async Task AddAsync(MessageEntity message) =>
        await context.Messages.AddAsync(message);

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();
}
