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
    IResourceAccessContext resourceAccess)
    : ResourceScopedDbContext(options, outboxOptions, provider, tenantContext, resourceAccess, Schema.Name)
{
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<VerifyPaymentEntity> VerifyPayments => Set<VerifyPaymentEntity>();
    public DbSet<ConcertAvailabilityEntity> ConcertAvailabilities => Set<ConcertAvailabilityEntity>();
    public DbSet<ApplicationAccessGrant> ApplicationAccessGrants => Set<ApplicationAccessGrant>();

    public ResourceCoverage OperationsCoverage => CoverageFor(TenantPermission.OperationsView);
    public ResourceCoverage TermsCoverage => CoverageFor(TenantPermission.TermsRead);

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationAccessGrant>().HasQueryFilter(TenantFilters.Key,
            ResourceAccessExpressions.LiveForCoverage<ApplicationAccessGrant, ApplicationAccessScope>(
                    this,
                    () => OperationsCoverage,
                    ApplicationAccessScope.Summary)
                .Or(ResourceAccessExpressions.LiveForCoverage<ApplicationAccessGrant, ApplicationAccessScope>(
                    this,
                    () => TermsCoverage,
                    ApplicationAccessScope.Proposal)));

        modelBuilder.Entity<ApplicationEntity>().HasQueryFilter(TenantFilters.Key, application =>
            ApplicationAccessGrants.Any(grant =>
                grant.ResourceId == application.Id && grant.Scope == ApplicationAccessScope.Summary));
    }
}
