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

    public ResourceAudience OperationsAudience => AudienceFor(TenantPermission.OperationsView);
    public ResourceAudience TermsAudience => AudienceFor(TenantPermission.TermsRead);

    /* The availability projection is deliberately unfiltered: it answers whether a date is taken and
       nothing else, so there is nothing tenant-private in it to protect. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApplicationAccessGrant>().HasQueryFilter(TenantFilters.Key,
            ResourceAccessExpressions.LiveForAudience<ApplicationAccessGrant, ApplicationAccessScope>(
                    this,
                    _ => OperationsAudience,
                    ApplicationAccessScope.Summary)
                .Or(ResourceAccessExpressions.LiveForAudience<ApplicationAccessGrant, ApplicationAccessScope>(
                    this,
                    _ => TermsAudience,
                    ApplicationAccessScope.Proposal)));

        modelBuilder.Entity<ApplicationEntity>().HasQueryFilter(TenantFilters.Key, application =>
            ApplicationAccessGrants.Any(grant =>
                grant.ResourceId == application.Id && grant.Scope == ApplicationAccessScope.Summary));
    }
}
