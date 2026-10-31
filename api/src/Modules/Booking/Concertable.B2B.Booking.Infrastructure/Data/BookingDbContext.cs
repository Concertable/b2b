using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal sealed class BookingDbContext(
    DbContextOptions<BookingDbContext> options,
    IOptions<OutboxOptions> outboxOptions,
    BookingConfigurationProvider provider,
    ITenantContext tenantContext,
    IAuthorizationContext authorization,
    TimeProvider timeProvider)
    : ResourceScopedDbContext(options, outboxOptions, provider, tenantContext, authorization, timeProvider, Schema.Name)
{
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<ContractEntity> Contracts => Set<ContractEntity>();

    public ResourceAccess OperationsAccess => ActorAccess(TenantPermission.OperationsView);
    public ResourceAccess TermsAccess => ActorAccess(TenantPermission.TermsRead);

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingEntity>().HasQueryFilter(TenantFilters.Key, booking =>
            OperationsAccess == ResourceAccess.All
            && (booking.VenueTenantId == ActorTenantId || booking.ArtistTenantId == ActorTenantId));

        modelBuilder.Entity<ContractEntity>().HasQueryFilter(TenantFilters.Key, contract =>
            TermsAccess == ResourceAccess.All
            && (contract.VenueTenantId == ActorTenantId || contract.ArtistTenantId == ActorTenantId));
    }
}
