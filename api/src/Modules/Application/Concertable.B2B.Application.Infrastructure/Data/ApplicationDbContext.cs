using Concertable.B2B.Application.Contracts.Enums;
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
    public DbSet<ApplicationAccessGrant> ApplicationAccessGrants => Set<ApplicationAccessGrant>();

    public ResourceAccess OperationsAccess =>
        Membership?.Permissions[TenantPermission.OperationsView] ?? ResourceAccess.None;
    public ResourceAccess TermsAccess =>
        Membership?.Permissions[TenantPermission.TermsRead] ?? ResourceAccess.None;

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationAccessGrant>().HasQueryFilter(TenantFilters.Key,
            IsVisibleGrant<ApplicationAccessGrant, ApplicationAccessScope>(
                    () => OperationsAccess,
                    ApplicationAccessScope.Summary)
                .Or(IsVisibleGrant<ApplicationAccessGrant, ApplicationAccessScope>(
                    () => TermsAccess,
                    ApplicationAccessScope.Proposal)));

        modelBuilder.Entity<ApplicationEntity>().HasQueryFilter(TenantFilters.Key, application =>
            ApplicationAccessGrants.Any(grant =>
                grant.ResourceId == application.Id && grant.Scope == ApplicationAccessScope.Summary));
    }
}
