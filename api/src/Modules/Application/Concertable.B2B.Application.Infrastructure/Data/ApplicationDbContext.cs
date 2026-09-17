using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ApplicationConfigurationProvider provider,
    ITenantContext tenantContext,
    IResourceAccessContext resourceAccess)
    : ResourceScopedDbContext(options, provider, tenantContext, resourceAccess, Schema.Name)
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
            ResourceAccessExpressions.LiveForCurrentMember<ApplicationAccessGrant, ApplicationAccessScope>(this)
                .And(grant =>
                    grant.Scope == ApplicationAccessScope.Summary
                        && (OperationsAudience == ResourceAudience.TenantResources
                                && (grant.MembershipId == null || grant.MembershipId == ActiveMembershipId)
                            || OperationsAudience == ResourceAudience.AssignedResources
                                && grant.MembershipId == ActiveMembershipId)
                    || grant.Scope == ApplicationAccessScope.Proposal
                        && (TermsAudience == ResourceAudience.TenantResources
                                && (grant.MembershipId == null || grant.MembershipId == ActiveMembershipId)
                            || TermsAudience == ResourceAudience.AssignedResources
                                && grant.MembershipId == ActiveMembershipId)));

        modelBuilder.Entity<ApplicationEntity>().HasQueryFilter(TenantFilters.Key, application =>
            ApplicationAccessGrants.Any(grant =>
                grant.ResourceId == application.Id && grant.Scope == ApplicationAccessScope.Summary));
    }
}
