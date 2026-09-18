using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

// Keyed by its issuing tenant, not a surrogate id, so no generic CRUD base binds.
internal interface IInvoiceSequenceRepository
{
    Task<InvoiceSequenceEntity?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);

    Task InsertAsync(InvoiceSequenceEntity sequence, CancellationToken ct = default);
}
