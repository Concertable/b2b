using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoiceRepository(ConcertDbContext context) : IInvoiceRepository
{
    public Task<InvoiceEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default) =>
        context.Invoices.AsNoTracking().SingleOrDefaultAsync(invoice => invoice.ConcertId == concertId, ct);
}
