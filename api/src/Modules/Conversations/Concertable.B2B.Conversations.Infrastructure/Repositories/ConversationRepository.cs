using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class ConversationRepository : Repository<ConversationEntity>, IConversationRepository
{
    private readonly ConversationsDbContext context;

    public ConversationRepository(ConversationsDbContext context) : base(context)
    {
        this.context = context;
    }

    public new Task<ConversationEntity?> GetByIdAsync(int conversationId, CancellationToken ct = default) =>
        context.Conversations.AsNoTracking().SingleOrDefaultAsync(conversation => conversation.Id == conversationId, ct);
}

internal sealed class ConversationPrivilegedRepository : IConversationPrivilegedRepository
{
    private readonly ConversationsPrivilegedDbContext context;

    public ConversationPrivilegedRepository(ConversationsPrivilegedDbContext context)
    {
        this.context = context;
    }

    public Task<ConversationEntity?> GetWithGrantsByIdAsync(int conversationId, CancellationToken ct = default) =>
        context.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.AccessGrants)
            .SingleOrDefaultAsync(conversation => conversation.Id == conversationId, ct);

    public async Task<ConversationEntity?> GetWithGrantsByIdForUpdateAsync(
        int conversationId,
        CancellationToken ct = default)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM conversations."Conversations"
            WHERE "Id" = {conversationId}
            FOR UPDATE
            """, ct);
        return await context.Conversations
            .Include(conversation => conversation.AccessGrants)
            .SingleOrDefaultAsync(conversation => conversation.Id == conversationId, ct);
    }

    public async Task<ConversationCreationReceipt?> GetCreationReceiptForUpdateAsync(
        Guid creatorTenantId,
        Guid createdByMembershipId,
        Guid requestId,
        CancellationToken ct = default)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM conversations."ConversationCreationReceipts"
            WHERE "CreatorTenantId" = {creatorTenantId}
              AND "CreatedByMembershipId" = {createdByMembershipId}
              AND "RequestId" = {requestId}
            FOR UPDATE
            """, ct);
        return await context.ConversationCreationReceipts.SingleOrDefaultAsync(
            receipt => receipt.CreatorTenantId == creatorTenantId
                       && receipt.CreatedByMembershipId == createdByMembershipId
                       && receipt.RequestId == requestId,
            ct);
    }

    public async Task<MessageEntity?> GetMessageReceiptForUpdateAsync(
        int conversationId,
        Guid sentByMembershipId,
        Guid requestId,
        CancellationToken ct = default)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            SELECT 1
            FROM conversations."Messages"
            WHERE "ConversationId" = {conversationId}
              AND "SentByMembershipId" = {sentByMembershipId}
              AND "RequestId" = {requestId}
            FOR UPDATE
            """, ct);
        return await context.Messages.SingleOrDefaultAsync(
            message => message.ConversationId == conversationId
                       && message.SentByMembershipId == sentByMembershipId
                       && message.RequestId == requestId,
            ct);
    }

    public async Task<IReadOnlyDictionary<Guid, TenantDisplay>> GetTenantDisplaysAsync(
        IReadOnlySet<Guid> tenantIds,
        CancellationToken ct = default) =>
        await context.TenantDisplays
            .AsNoTracking()
            .Where(display => tenantIds.Contains(display.TenantId))
            .ToDictionaryAsync(display => display.TenantId, ct);

    public void Add(ConversationEntity conversation) => context.Conversations.Add(conversation);
    public void AddAccessGrants(IEnumerable<ConversationAccessGrant> grants) =>
        context.ConversationAccessGrants.AddRange(grants);
    public void Add(ConversationCreationReceipt receipt) => context.ConversationCreationReceipts.Add(receipt);
    public void Add(MessageEntity message) => context.Messages.Add(message);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
