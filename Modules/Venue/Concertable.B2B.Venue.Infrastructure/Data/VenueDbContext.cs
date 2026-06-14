using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueDbContext(
    DbContextOptions<VenueDbContext> options,
    VenueConfigurationProvider provider,
    ITenantContext tenantContext)
    : TenantScopedDbContext(options, provider, tenantContext, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
    public DbSet<VenueImageEntity> VenueImages => Set<VenueImageEntity>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
    public DbSet<VenueReview> VenueReviews => Set<VenueReview>();

    /* Venue and its images are owned by one tenant; the rating projection and reviews are public
       aggregates, deliberately unfiltered. Public browse of any venue is served by PublicVenueDbContext. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySingleOwner<VenueEntity>(this);
        modelBuilder.ApplySingleOwner<VenueImageEntity>(this);
    }
}
