using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Contracts;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenueCommandFacts(
    VenuePrivilegedDbContext context,
    CommandTransactionAccessor transactions) : IVenueCommandFacts
{
    public async Task<VenueProfile?> GetByIdAsync(int venueId, CancellationToken ct = default)
    {
        await (transactions.Current
            ?? throw new InvalidOperationException("Venue command facts require an active command transaction."))
            .EnlistAsync(context, ct);

        return await context.Venues
            .Where(venue => venue.Id == venueId)
            .ToProfiles()
            .SingleOrDefaultAsync(ct);
    }
}
