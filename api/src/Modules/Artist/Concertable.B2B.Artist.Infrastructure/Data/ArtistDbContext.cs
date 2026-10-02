using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Artist.Infrastructure.Data;

internal sealed class ArtistDbContext(
    DbContextOptions<ArtistDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ArtistConfigurationProvider provider,
    ITenantContext tenantContext)
    : TenantScopedDbContext(options, outboxOptions, provider, tenantContext, Schema.Name)
{
    public DbSet<ArtistEntity> Artists => Set<ArtistEntity>();
    public DbSet<ArtistRatingProjection> ArtistRatingProjections => Set<ArtistRatingProjection>();
    public DbSet<ArtistReview> ArtistReviews => Set<ArtistReview>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplySingleOwner<ArtistEntity>(this);
    }
}
