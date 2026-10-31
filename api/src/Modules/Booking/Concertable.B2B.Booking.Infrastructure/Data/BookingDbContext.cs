using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Booking.Contracts.Enums;
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
    public DbSet<BookingAccessGrant> BookingAccessGrants => Set<BookingAccessGrant>();
    public DbSet<ContractAccessGrant> ContractAccessGrants => Set<ContractAccessGrant>();

    public ResourceAccess OperationsAccess =>
        Membership?.Permissions[TenantPermission.OperationsView] ?? ResourceAccess.None;
    public ResourceAccess TermsAccess =>
        Membership?.Permissions[TenantPermission.TermsRead] ?? ResourceAccess.None;

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BookingAccessGrant>().HasQueryFilter(TenantFilters.Key,
            IsVisibleGrant<BookingAccessGrant, BookingAccessScope>(
                () => OperationsAccess,
                BookingAccessScope.Summary,
                BookingAccessScope.Operations));

        modelBuilder.Entity<ContractAccessGrant>().HasQueryFilter(TenantFilters.Key,
            IsVisibleGrant<ContractAccessGrant, ContractAccessScope>(
                () => TermsAccess,
                ContractAccessScope.Read));

        modelBuilder.Entity<BookingEntity>().HasQueryFilter(TenantFilters.Key, booking =>
            BookingAccessGrants.Any(grant =>
                grant.ResourceId == booking.Id && grant.Scope == BookingAccessScope.Summary));

        modelBuilder.Entity<ContractEntity>().HasQueryFilter(TenantFilters.Key, contract =>
            ContractAccessGrants.Any(grant =>
                grant.ResourceId == contract.Id && grant.Scope == ContractAccessScope.Read));
    }
}
