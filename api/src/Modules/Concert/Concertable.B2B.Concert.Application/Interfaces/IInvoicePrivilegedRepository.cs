using Concertable.B2B.Concert.Domain.Entities;
using Concertable.DataAccess.Application;

namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IInvoicePrivilegedRepository : IRepository<InvoiceEntity>
{
    Task<bool> ExistsByBookingIdAsync(int bookingId, CancellationToken ct = default);
}
