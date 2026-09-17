using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoicePrivilegedRepository : PrivilegedRepository<InvoiceEntity>, IInvoicePrivilegedRepository
{
    private readonly ConcertPrivilegedDbContext context;

    public InvoicePrivilegedRepository(ConcertPrivilegedDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<bool> ExistsByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Invoices.AnyAsync(invoice => invoice.BookingId == bookingId, ct);
}
