using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Deal.Application.Interfaces;

internal interface IDealRepository : ITenantScopedRepository<DealEntity>
{
    Task<IEnumerable<DealEntity>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);
}
