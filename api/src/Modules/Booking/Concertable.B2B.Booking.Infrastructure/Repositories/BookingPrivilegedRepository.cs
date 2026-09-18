using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.Booking.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Repositories;

internal sealed class BookingPrivilegedRepository(BookingPrivilegedDbContext context)
    : IBookingPrivilegedRepository
{
    public Task<BookingEntity?> GetByIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Bookings.SingleOrDefaultAsync(booking => booking.Id == bookingId, ct);

    public Task<BookingEntity?> GetWithContractByIdAsync(int bookingId, CancellationToken ct = default) =>
        context.Bookings
            .Include(booking => booking.Contract)
            .SingleOrDefaultAsync(booking => booking.Id == bookingId, ct);

    public Task<int?> GetIdByApplicationIdAsync(
        int applicationId,
        CancellationToken ct = default) =>
        context.Bookings
            .Where(booking => booking.ApplicationId == applicationId)
            .Select(booking => (int?)booking.Id)
            .SingleOrDefaultAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
