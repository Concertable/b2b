using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Venue.Contracts;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Repositories;

internal sealed class VenuePrivilegedReadRepository(
    VenuePrivilegedDbContext context,
    UnitOfWorkAccessor unitOfWorkAccessor) : IVenuePrivilegedReadRepository
{
    public async Task<VenueProfile?> GetByIdAsync(int venueId, CancellationToken ct = default)
    {
        await (unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Venue privileged queries require an active transaction."))
            .EnlistAsync(context, ct);

        return await context.Venues
            .Where(venue => venue.Id == venueId)
            .ToProfiles()
            .SingleOrDefaultAsync(ct);
    }
}
