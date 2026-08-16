using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Repositories;

internal sealed class InvoiceRepository : VenueArtistTenantScopedRepository<InvoiceEntity>, IInvoiceRepository
{
    public InvoiceRepository(ConcertDbContext context) : base(context) { }

    public Task<InvoiceEntity?> GetByConcertIdAsync(int concertId, CancellationToken ct = default) =>
        context.Invoices
            .FirstOrDefaultAsync(i => context.Concerts.Any(c => c.Id == concertId && c.BookingId == i.BookingId), ct);

    public Task<InvoiceEntity?> GetByApplicationIdAsync(int applicationId, CancellationToken ct = default) =>
        context.Invoices
            .FirstOrDefaultAsync(i => i.Booking.ApplicationId == applicationId, ct);
}
