using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IConcertPrivilegedRepository : IRepository<ConcertEntity>
{
    Task<ConcertEntity?> GetByIdForUpdateAsync(int concertId, CancellationToken ct = default);

    Task<ConcertEntity?> GetWithGrantsByIdAsync(int concertId, CancellationToken ct = default);

    Task<ConcertAccessIdentity?> GetIdentityByIdForUpdateAsync(int concertId, CancellationToken ct = default);
}
