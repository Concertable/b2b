using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISequenceRepository<TSequence>
    where TSequence : class, ISequence<TSequence>
{
    /// <summary>
    /// Allocates the next gap-free number for a tenant, lazily creating the counter on first use. The
    /// increment is tracked on the shared context and flushed by the caller's own SaveChanges, so a number
    /// is only ever consumed if that transaction commits.
    /// </summary>
    Task<long> AllocateNextAsync(Guid tenantId, CancellationToken ct = default);
}
