using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal interface IInvoiceRepository
{
    Task<InvoiceEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default);
}
