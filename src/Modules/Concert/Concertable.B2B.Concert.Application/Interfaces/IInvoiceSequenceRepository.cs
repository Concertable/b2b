namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceSequenceRepository
{
    /// <summary>
    /// Allocates the next gap-free number for a supplier tenant, lazily creating the counter on the first
    /// invoice. The increment is tracked on the shared context and flushed by the settlement's own
    /// SaveChanges, so the number is only ever consumed if the invoice actually commits.
    /// </summary>
    Task<long> AllocateNextAsync(Guid tenantId, CancellationToken ct = default);
}
