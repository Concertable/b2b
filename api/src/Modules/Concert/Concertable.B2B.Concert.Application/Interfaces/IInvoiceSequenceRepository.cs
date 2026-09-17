using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The per-supplier invoice counter. Keyed by its issuing tenant rather than a surrogate id, so it takes no
/// generic CRUD base: the only two operations are finding a supplier's row and starting one.
/// </summary>
internal interface IInvoiceSequenceRepository
{
    Task<InvoiceSequenceEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    Task InsertAsync(InvoiceSequenceEntity sequence, CancellationToken ct = default);
}
