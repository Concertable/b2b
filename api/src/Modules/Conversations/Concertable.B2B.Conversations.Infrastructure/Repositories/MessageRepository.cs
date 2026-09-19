using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessageRepository : IMessageRepository
{
    private static readonly Expression<Func<MessageEntity, bool>> NotHidden =
        message => message.HiddenAt == null || message.RestoredAt != null && message.RestoredAt > message.HiddenAt;
    private readonly ConversationsDbContext context;

    public MessageRepository(ConversationsDbContext context)
    {
        this.context = context;
    }

    public Task<MessageEntity?> GetByIdAsync(int messageId, CancellationToken ct = default) =>
        context.Messages.AsNoTracking().Where(NotHidden)
            .SingleOrDefaultAsync(message => message.Id == messageId, ct);

    public async Task<IReadOnlyList<MessageEntity>> GetByConversationIdAsync(
        int conversationId,
        CancellationToken ct = default) =>
        await context.Messages
            .AsNoTracking()
            .Where(NotHidden)
            .Where(message => message.ConversationId == conversationId)
            .OrderBy(message => message.Sequence)
            .ToListAsync(ct);

    public Task<bool> ContainsSequenceAsync(
        int conversationId,
        long sequence,
        CancellationToken ct = default) =>
        context.Messages.Where(NotHidden).AnyAsync(
            message => message.ConversationId == conversationId && message.Sequence == sequence,
            ct);

    public Task<int> GetUnreadCountAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken ct = default) =>
        (from message in context.Messages.Where(NotHidden)
         join position in context.ConversationReadPositions.Where(position =>
                 position.TenantId == tenantId && position.MembershipId == membershipId)
             on message.ConversationId equals position.ConversationId into positions
         from position in positions.DefaultIfEmpty()
         where message.SenderTenantId != tenantId
               && (position == null || message.Sequence > position.LastReadSequence)
         select message.Id).CountAsync(ct);

    public async Task<IReadOnlyList<MessagePreview>> GetRecentPreviewsAsync(
        Guid tenantId,
        Guid membershipId,
        CancellationToken ct = default)
    {
        var visible = context.Messages.Where(NotHidden);
        var latestIds = visible
            .GroupBy(message => message.ConversationId)
            .Select(group => group.OrderByDescending(message => message.Sequence).Select(message => message.Id).First());

        return await visible
            .AsNoTracking()
            .Where(message => latestIds.Contains(message.Id))
            .OrderByDescending(message => message.SentAt)
            .Take(5)
            .Select(message => new MessagePreview(
                message.Id,
                message.ConversationId,
                message.Content,
                message.SentAt,
                message.SenderTenantId != tenantId
                    && !context.ConversationReadPositions.Any(position =>
                        position.ConversationId == message.ConversationId
                        && position.TenantId == tenantId
                        && position.MembershipId == membershipId
                        && position.LastReadSequence >= message.Sequence)))
            .ToListAsync(ct);
    }
}
