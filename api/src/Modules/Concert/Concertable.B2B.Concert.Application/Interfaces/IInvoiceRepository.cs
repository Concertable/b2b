using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoiceRepository : IRepository<InvoiceEntity, int>
{
    Task<InvoiceEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default);
}
