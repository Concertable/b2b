using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Concert persistence for work no human is acting in, and for a command that must administer the whole ACL.
/// Its consumers are the module's own fenced services; no controller or query service can resolve it.
/// </summary>
internal interface IConcertPrivilegedRepository : IRepository<ConcertEntity>
{
    Task<ConcertEntity?> GetByIdForUpdateAsync(int concertId, CancellationToken ct = default);

    Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default);

    /// <summary>The principals and access version alone, under this concert's update lock — what a share or
    /// assignment command authorises against before it loads the rest.</summary>
    Task<ConcertAccessIdentity?> GetIdentityByIdForUpdateAsync(int concertId, CancellationToken ct = default);
}
