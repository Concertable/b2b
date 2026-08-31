using Concertable.B2B.Booking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Extensions;

internal static class DbUpdateExceptionExtensions
{
    extension(DbUpdateException exception)
    {
        public bool IsBookingConcurrencyConflict() =>
            exception is DbUpdateConcurrencyException &&
            exception.Entries.Any(entry => entry.Entity is BookingEntity);
    }
}
