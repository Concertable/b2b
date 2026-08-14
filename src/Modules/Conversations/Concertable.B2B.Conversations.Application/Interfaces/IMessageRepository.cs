using Concertable.Contracts;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessageRepository
{
    /// <summary>Every message in the tenant's threads (both directions), newest first — the tenant's inbox rows.</summary>
    Task<IPagination<MessageEntity>> GetByTenantIdAsync(Guid tenantId, IPageParams pageParams);

    /// <summary>Count of a tenant's received messages past the member's per-thread read pointer.</summary>
    Task<int> GetUnreadCountByTenantIdAsync(Guid tenantId, Guid userId);

    /// <summary>Advance (or create) the member's read pointer to <paramref name="readAt"/> for every thread the
    /// tenant is party to — marking the whole inbox read for that member.</summary>
    Task AdvanceReadPointersAsync(Guid tenantId, Guid userId, DateTime readAt);
    Task<IReadOnlyDictionary<Guid, ParticipantProfile>> GetParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds);

    Task AddAsync(MessageEntity message);
    Task SaveChangesAsync();
}
