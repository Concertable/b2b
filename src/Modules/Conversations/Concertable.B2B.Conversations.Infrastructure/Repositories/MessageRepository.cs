using Concertable.Contracts;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessageRepository : Repository<MessageEntity>, IMessageRepository
{
    private static readonly Expression<Func<MessageEntity, bool>> NotHidden =
        m => m.HiddenAt == null || (m.RestoredAt != null && m.RestoredAt > m.HiddenAt);
    private readonly ConversationsDbContext context;

    public MessageRepository(ConversationsDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<IPagination<MessageEntity>> GetByTenantIdAsync(Guid tenantId, IPageParams pageParams) =>
        context.Messages
            .Where(NotHidden)
            .OrderByDescending(m => m.SentDate)
            .ToPaginationAsync(pageParams);

    public Task<int> GetUnreadCountByTenantIdAsync(Guid tenantId, Guid userId) =>
        (from m in context.Messages.Where(m => m.SenderTenantId != tenantId).Where(NotHidden)
         join p in context.ThreadReadStates.Where(p => p.UserId == userId)
             on new { m.VenueTenantId, m.ArtistTenantId } equals new { p.VenueTenantId, p.ArtistTenantId } into pointers
         from p in pointers.DefaultIfEmpty()
         where p == null || m.SentDate > p.LastReadAt
         select m.Id)
        .CountAsync();

    public async Task<IReadOnlyList<MessagePreview>> GetRecentPreviewsAsync(Guid tenantId, Guid userId)
    {
        var tenantMessages = context.Messages
            .Where(NotHidden)
            .Where(m => m.VenueTenantId == tenantId || m.ArtistTenantId == tenantId);
        var latestMessageIds = tenantMessages
            .GroupBy(m => new { m.VenueTenantId, m.ArtistTenantId })
            .Select(group => group
                .OrderByDescending(m => m.SentDate)
                .ThenByDescending(m => m.Id)
                .Select(m => m.Id)
                .First());

        return await tenantMessages
            .AsNoTracking()
            .Where(m => latestMessageIds.Contains(m.Id))
            .OrderByDescending(m => m.SentDate)
            .ThenByDescending(m => m.Id)
            .Take(5)
            .Select(m => new MessagePreview(
                m.Id,
                m.VenueTenantId == tenantId ? m.ArtistTenantId : m.VenueTenantId,
                m.VenueTenantId != tenantId,
                m.Content,
                m.SentDate,
                context.Messages.Where(NotHidden).Any(candidate =>
                    candidate.VenueTenantId == m.VenueTenantId
                    && candidate.ArtistTenantId == m.ArtistTenantId
                    && candidate.SenderTenantId != tenantId
                    && !context.ThreadReadStates.Any(pointer =>
                        pointer.UserId == userId
                        && pointer.VenueTenantId == candidate.VenueTenantId
                        && pointer.ArtistTenantId == candidate.ArtistTenantId
                        && pointer.LastReadAt >= candidate.SentDate))))
            .ToListAsync();
    }

    public async Task<IReadOnlyDictionary<Guid, ParticipantProfile>> GetParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds) =>
        await context.ParticipantProfiles
            .Where(p => tenantIds.Contains(p.TenantId))
            .ToDictionaryAsync(p => p.TenantId);

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
}
