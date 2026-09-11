using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Extensions;

internal static class DbUpdateExceptionExtensions
{
    extension(DbUpdateException exception)
    {
        public bool IsConcertConcurrencyConflict(int concertId) =>
            exception is DbUpdateConcurrencyException &&
            exception.Entries.Any(entry =>
                entry.Entity is ConcertEntity concert && concert.Id == concertId);
    }
}
