using Concertable.Contracts;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageRepository
{
    /// <summary>Messages a tenant received (its counterpart sent), newest first — the tenant's inbox rows.</summary>
    Task<IPagination<MessageEntity>> GetByTenantIdAsync(Guid tenantId, IPageParams pageParams);

    /// <summary>Count of a tenant's received messages past the member's per-thread read pointer.</summary>
    Task<int> GetUnreadCountByTenantIdAsync(Guid tenantId, Guid userId);

    /// <summary>Advance (or create) the member's read pointer for the thread with <paramref name="counterpartTenantId"/>.</summary>
    Task AdvanceReadPointerAsync(Guid tenantId, Guid counterpartTenantId, Guid userId, DateTime readAt);

    Task AddAsync(MessageEntity message);
    Task SaveChangesAsync();
}
