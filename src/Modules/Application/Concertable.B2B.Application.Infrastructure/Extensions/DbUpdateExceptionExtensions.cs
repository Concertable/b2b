using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Extensions;

internal static class DbUpdateExceptionExtensions
{
    extension(DbUpdateException exception)
    {
        public bool IsApplicationConcurrencyConflict() =>
            exception is DbUpdateConcurrencyException && TouchesApplication(exception);

        public bool IsApplicationAcceptanceConflict() =>
            TouchesApplication(exception) &&
            (exception is DbUpdateConcurrencyException || exception.IsDuplicateKey());
    }

    private static bool TouchesApplication(DbUpdateException exception) =>
        exception.Entries.Any(entry => entry.Entity is ApplicationEntity);
}
