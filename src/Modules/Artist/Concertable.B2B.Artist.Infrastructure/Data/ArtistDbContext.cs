using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistDbContext(
    DbContextOptions<ArtistDbContext> options,
    ArtistConfigurationProvider provider,
    ITenantContext tenantContext)
    : TenantScopedDbContext(options, provider, tenantContext, Schema.Name)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<ArtistReview> ArtistReviews => Set<ArtistReview>();

    /* The artist row is tenant-owned; its rating projection and reviews are public aggregates,
       deliberately unfiltered. Public browse of any artist is served by PublicArtistDbContext. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySingleOwner<ArtistEntity>(this);
    }
}
