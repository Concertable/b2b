using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    ApplicationConfigurationProvider provider,
    ITenantContext tenantContext,
    IAuthorizationContext authorization,
    TimeProvider timeProvider)
    : ResourceScopedDbContext(options, outboxOptions, provider, tenantContext, authorization, timeProvider, Schema.Name)
{
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<VerifyPaymentEntity> VerifyPayments => Set<VerifyPaymentEntity>();
    public DbSet<ConcertAvailabilityEntity> ConcertAvailabilities => Set<ConcertAvailabilityEntity>();

    public ResourceAccess OperationsAccess => ActorAccess(TenantPermission.OperationsView);

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationEntity>().HasQueryFilter(TenantFilters.Key, application =>
            OperationsAccess == ResourceAccess.All
            && (application.VenueTenantId == ActorTenantId || application.ArtistTenantId == ActorTenantId));
    }
}
