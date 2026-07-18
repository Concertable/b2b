namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface ISequenceRepository
{
    /// <summary>
    /// Allocates the next gap-free number for an owner, lazily creating the counter on first use. The
    /// increment is tracked on the shared context and flushed by the caller's own SaveChanges, so a number
    /// is only ever consumed if that transaction commits.
    /// </summary>
    Task<long> AllocateNextAsync(Guid ownerId, CancellationToken ct = default);
}
