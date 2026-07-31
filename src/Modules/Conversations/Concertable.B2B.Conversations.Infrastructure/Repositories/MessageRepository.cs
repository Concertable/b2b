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

    public Task<IPagination<MessageEntity>> GetByTenantIdAsync(Guid tenantId, IPageParams pageParams) =>
        context.Messages
            .Where(m => m.SenderTenantId != tenantId)
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

    public async Task AdvanceReadPointerAsync(Guid tenantId, Guid counterpartTenantId, Guid userId, DateTime readAt)
    {
        var pair = await context.Messages
            .Where(m => (m.VenueTenantId == tenantId && m.ArtistTenantId == counterpartTenantId)
                     || (m.VenueTenantId == counterpartTenantId && m.ArtistTenantId == tenantId))
            .Select(m => new { m.VenueTenantId, m.ArtistTenantId })
            .FirstOrDefaultAsync();

        if (pair is null)
            return;

        var pointer = await context.ThreadReadStates.FirstOrDefaultAsync(p =>
            p.VenueTenantId == pair.VenueTenantId && p.ArtistTenantId == pair.ArtistTenantId && p.UserId == userId);

        if (pointer is null)
            await context.ThreadReadStates.AddAsync(
                ThreadReadStateEntity.Create(pair.VenueTenantId, pair.ArtistTenantId, userId, readAt));
        else
            pointer.Advance(readAt);

        await context.SaveChangesAsync();
    }

    public async Task AddAsync(MessageEntity message) =>
        await context.Messages.AddAsync(message);

    public async Task SaveChangesAsync() =>
        await context.SaveChangesAsync();
}
