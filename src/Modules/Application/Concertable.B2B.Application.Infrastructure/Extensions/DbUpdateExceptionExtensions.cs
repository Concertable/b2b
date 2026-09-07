using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Extensions;

internal static class DbUpdateExceptionExtensions
{
    extension(DbUpdateException exception)
    {
        public bool IsApplicationConcurrencyConflict(int applicationId) =>
            exception is DbUpdateConcurrencyException && Touches(exception, applicationId);

        public bool IsApplicationAcceptanceConflict(int applicationId) =>
            Touches(exception, applicationId) &&
            (exception is DbUpdateConcurrencyException || exception.IsDuplicateKey());
    }

    // Scoped to the row, not the type: acceptance also rejects the opportunity's other applications in the
    // same unit of work, and a sibling losing its own race must not be absorbed as this operation's failure.
    private static bool Touches(DbUpdateException exception, int applicationId) =>
        exception.Entries.Any(entry =>
            entry.Entity is ApplicationEntity application && application.Id == applicationId);
}
