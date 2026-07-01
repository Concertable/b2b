using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

/// <summary>
/// The Venue module's platform-admin stance: the same anemic configuration as <see cref="VenueDbContext"/>,
/// writable, with no tenant filter — used by the admin approval flow (an admin approves venues they don't
/// own). The tenant-filtered counterpart is <see cref="VenueDbContext"/>; the unfiltered read-only one is
/// <see cref="PublicVenueDbContext"/>.
/// </summary>
internal sealed class AdminVenueDbContext(
    DbContextOptions<AdminVenueDbContext> options,
    VenueConfigurationProvider provider)
    : AdminDbContext(options, provider, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
}
