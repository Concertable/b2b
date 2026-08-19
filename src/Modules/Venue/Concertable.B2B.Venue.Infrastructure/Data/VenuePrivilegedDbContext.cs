using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenuePrivilegedDbContext(
    DbContextOptions<VenuePrivilegedDbContext> options,
    VenueConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
}
