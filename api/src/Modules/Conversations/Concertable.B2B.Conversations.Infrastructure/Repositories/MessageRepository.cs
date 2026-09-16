using Concertable.Contracts;
using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessageRepository : Repository<MessageEntity>, IMessageRepository
{
    private static readonly Expression<Func<MessageEntity, bool>> NotHidden =
        m => m.HiddenAt == null || (m.RestoredAt != null && m.RestoredAt > m.HiddenAt);
    private readonly ConversationsDbContext context;

    public MessageRepository(ConversationsDbContext context)
        : base(context)
    {
        this.context = context;
    }

    /* Stated rather than inherited from the ambient filter: these queries are named for the tenant they
       serve, and a reader should see which threads that means without knowing the context's stance. The
       filter still applies underneath, so the two have to agree. */
    private IQueryable<int> ThreadIdsOf(Guid tenantId) =>
        context.ThreadAccessGrants
            .Where(grant =>
                grant.TenantId == tenantId
                && grant.Facet == ThreadAccessFacet.Read
                && grant.RevokedAt == null)
            .Select(grant => grant.ResourceId);

    public Task<IPagination<MessageEntity>> GetByTenantIdAsync(Guid tenantId, IPageParams pageParams) =>
        context.Messages
            .Where(NotHidden)
            .Where(m => ThreadIdsOf(tenantId).Contains(m.ThreadId))
            .OrderByDescending(m => m.SentDate)
            .ToPaginationAsync(pageParams);

    public Task<int> GetUnreadCountByTenantIdAsync(Guid tenantId, Guid userId) =>
        (from m in context.Messages
             .Where(m => m.SenderTenantId != tenantId)
             .Where(NotHidden)
             .Where(m => ThreadIdsOf(tenantId).Contains(m.ThreadId))
         join p in context.ThreadReadStates.Where(p => p.UserId == userId && p.TenantId == tenantId)
             on m.ThreadId equals p.ThreadId into pointers
         from p in pointers.DefaultIfEmpty()
         where p == null || m.SentDate > p.LastReadAt
         select m.Id)
        .CountAsync();

    public async Task<IReadOnlyList<MessagePreview>> GetRecentPreviewsAsync(Guid tenantId, Guid userId)
    {
        var tenantMessages = context.Messages
            .Where(NotHidden)
            .Where(m => ThreadIdsOf(tenantId).Contains(m.ThreadId));

        var latestMessageIds = tenantMessages
            .GroupBy(m => m.ThreadId)
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
                m.ThreadId,
                context.ThreadAccessGrants
                    .Where(grant =>
                        grant.ResourceId == m.ThreadId
                        && grant.Facet == ThreadAccessFacet.Participate
                        && grant.TenantId != tenantId
                        && grant.RevokedAt == null)
                    .Select(grant => (Guid?)grant.TenantId)
                    .FirstOrDefault(),
                m.Content,
                m.SentDate,
                context.Messages.Where(NotHidden).Any(candidate =>
                    candidate.ThreadId == m.ThreadId
                    && candidate.SenderTenantId != tenantId
                    && !context.ThreadReadStates.Any(pointer =>
                        pointer.UserId == userId
                        && pointer.TenantId == tenantId
                        && pointer.ThreadId == candidate.ThreadId
                        && pointer.LastReadAt >= candidate.SentDate))))
            .ToListAsync();
    }

    public async Task<IReadOnlyDictionary<Guid, ParticipantProfile>> GetParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds) =>
        await context.ParticipantProfiles
            .Where(p => tenantIds.Contains(p.TenantId))
            .ToDictionaryAsync(p => p.TenantId);

    public async Task AdvanceReadPointersAsync(Guid tenantId, Guid userId, DateTime readAt)
    {
        var threadIds = await ThreadIdsOf(tenantId).Distinct().ToListAsync();

        var pointers = await context.ThreadReadStates
            .Where(p => p.UserId == userId && p.TenantId == tenantId)
            .ToDictionaryAsync(p => p.ThreadId);

        foreach (var threadId in threadIds)
        {
            if (pointers.TryGetValue(threadId, out var pointer))
                pointer.Advance(readAt);
            else
                await context.ThreadReadStates.AddAsync(
                    ThreadReadStateEntity.Create(threadId, tenantId, userId, readAt));
        }

        await context.SaveChangesAsync();
    }
}
