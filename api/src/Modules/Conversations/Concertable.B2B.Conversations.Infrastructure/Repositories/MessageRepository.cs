using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessageRepository : IMessageRepository
{
    private const int PreviewPageSize = 5;
    private static readonly Expression<Func<MessageEntity, bool>> NotHidden =
        message => message.HiddenAt == null || message.RestoredAt != null && message.RestoredAt > message.HiddenAt;
    private readonly ConversationsPrivilegedDbContext context;
    private readonly TimeProvider clock;

    public MessageRepository(ConversationsPrivilegedDbContext context, TimeProvider clock)
    {
        this.context = context;
        this.clock = clock;
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
        MembershipSnapshot actor,
        CancellationToken ct = default) =>
        (from message in ReadableMessages(actor)
         join position in context.ConversationReadPositions.Where(position =>
                 position.TenantId == actor.TenantId && position.MembershipId == actor.MembershipId)
             on message.ConversationId equals position.ConversationId into positions
         from position in positions.DefaultIfEmpty()
         where message.SenderTenantId != actor.TenantId
               && (position == null || message.Sequence > position.LastReadSequence)
         select message.Id).CountAsync(ct);

    public async Task<IReadOnlyList<MessagePreview>> GetRecentPreviewsAsync(
        MembershipSnapshot actor,
        int pageNumber,
        CancellationToken ct = default)
    {
        var visible = ReadableMessages(actor);
        var latestIds = visible
            .GroupBy(message => message.ConversationId)
            .Select(group => group.OrderByDescending(message => message.Sequence).Select(message => message.Id).First());

        return await visible
            .AsNoTracking()
            .Where(message => latestIds.Contains(message.Id))
            .OrderByDescending(message => message.SentAt)
            .ThenByDescending(message => message.Id)
            .Skip((pageNumber - 1) * PreviewPageSize)
            .Take(PreviewPageSize)
            .Select(message => new MessagePreview(
                message.Id,
                message.ConversationId,
                message.Content,
                message.SentAt,
                visible.Any(inbound =>
                    inbound.ConversationId == message.ConversationId
                    && inbound.SenderTenantId != actor.TenantId
                    && !context.ConversationReadPositions.Any(position =>
                        position.ConversationId == inbound.ConversationId
                        && position.TenantId == actor.TenantId
                        && position.MembershipId == actor.MembershipId
                        && position.LastReadSequence >= inbound.Sequence))))
            .ToListAsync(ct);
    }

    private IQueryable<MessageEntity> ReadableMessages(MembershipSnapshot actor)
    {
        var binding = ResourcePolicyBinding.FromCatalog(
            TenantPermission.MessagesRead, ResourceKind.Conversation, ResourceFacet.Read);
        if (binding.Policy != "conversation_grant" || binding.RequiredScopes.IsDefaultOrEmpty)
            throw new InvalidOperationException("Conversation Read binding is unsupported.");
        var now = clock.GetUtcNow().UtcDateTime;
        var visible = context.Messages.Where(NotHidden);
        foreach (var requiredScope in binding.RequiredScopes)
        {
            var scope = Enum.Parse<ConversationAccessScope>(requiredScope);
            var readable = ConversationGrantPolicy.Eligible(
                context.ConversationAccessGrants.AsNoTracking(),
                actor,
                binding.Permission,
                scope,
                now);
            visible = visible.Where(message =>
                readable.Any(grant => grant.ResourceId == message.ConversationId));
        }
        return visible;
    }
}
