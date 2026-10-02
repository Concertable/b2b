using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ApplicationConfigurationProvider provider,
    ITenantContext tenantContext)
    : TenantScopedDbContext(options, outboxOptions, provider, tenantContext, Schema.Name)
{
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<VerifyPaymentEntity> VerifyPayments => Set<VerifyPaymentEntity>();
    public DbSet<ConcertAvailabilityEntity> ConcertAvailabilities => Set<ConcertAvailabilityEntity>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVenueArtist<ApplicationEntity>(this);
        modelBuilder.ApplyVenueArtist<ConcertAvailabilityEntity>(this);
    }
}
