using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoiceRepository : Repository<InvoiceEntity>, IInvoiceRepository
{
    private readonly ConcertDbContext context;

    public InvoiceRepository(ConcertDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<InvoiceEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default) =>
        context.Invoices
            .SingleOrDefaultAsync(invoice => invoice.ConcertId == concertId, ct);
}
