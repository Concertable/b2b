using Concertable.Contracts;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Conversations.Application.Interfaces;

/// <summary>
/// Messages through the tenant-filtered context, so every read here is already scoped to the acting
/// tenant's own threads — an id belonging to a thread it is not party to reads as absent.
/// </summary>
internal interface IMessageRepository : IRepository<MessageEntity>
{
    /// <summary>Every message in the tenant's threads (both directions), newest first — the tenant's inbox rows.</summary>
    Task<IPagination<MessageEntity>> GetByTenantIdAsync(Guid tenantId, IPageParams pageParams);

    /// <summary>Count of a tenant's received messages past the member's per-thread read pointer.</summary>
    Task<int> GetUnreadCountByTenantIdAsync(Guid tenantId, Guid userId);

    /// <summary>Advance (or create) the member's read pointer to <paramref name="readAt"/> for every thread the
    /// tenant is party to — marking the whole inbox read for that member.</summary>
    Task AdvanceReadPointersAsync(Guid tenantId, Guid userId, DateTime readAt);
    Task<IReadOnlyDictionary<Guid, ParticipantProfile>> GetParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds);
}
