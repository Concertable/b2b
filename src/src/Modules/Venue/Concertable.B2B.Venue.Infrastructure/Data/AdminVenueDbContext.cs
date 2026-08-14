using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class AdminVenueDbContext(
    DbContextOptions<AdminVenueDbContext> options,
    VenueConfigurationProvider provider)
    : AdminDbContext(options, provider, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
}
