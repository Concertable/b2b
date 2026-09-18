using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceSequenceRepository
{
    Task<InvoiceSequenceEntity?> GetByTenantIdForUpdateAsync(Guid tenantId, CancellationToken ct = default);

    Task InsertAsync(InvoiceSequenceEntity sequence, CancellationToken ct = default);
}
