using Concertable.DataAccess.Application;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IMessagePrivilegedRepository : IRepository<MessageEntity>
{
    /// <summary>Every message a user authored, across all threads and tenants — the unfiltered stance GDPR
    /// erasure needs to sever the author link, and the export reads to return the subject's own message bodies.</summary>
    Task<IReadOnlyList<MessageEntity>> ListBySenderUserAsync(Guid userId, CancellationToken ct = default);
}
