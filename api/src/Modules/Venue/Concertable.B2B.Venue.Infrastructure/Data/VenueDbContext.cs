using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Venue.Infrastructure.Data;

internal sealed class VenueDbContext(
    DbContextOptions<VenueDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    VenueConfigurationProvider provider,
    ITenantContext tenantContext)
    : TenantScopedDbContext(options, outboxOptions, provider, tenantContext, Schema.Name)
{
    public DbSet<VenueEntity> Venues => Set<VenueEntity>();
    public DbSet<VenueImageEntity> VenueImages => Set<VenueImageEntity>();
    public DbSet<VenueRatingProjection> VenueRatingProjections => Set<VenueRatingProjection>();
    public DbSet<VenueReview> VenueReviews => Set<VenueReview>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySingleOwner<VenueEntity>(this);
        modelBuilder.ApplySingleOwner<VenueImageEntity>(this);
    }
}
