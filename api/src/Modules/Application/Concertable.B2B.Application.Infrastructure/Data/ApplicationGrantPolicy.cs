using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal static class ApplicationGrantPolicy
{
    public static IQueryable<ApplicationEntity> EligibleApplications(
        IQueryable<ApplicationEntity> applications,
        Guid tenantId,
        string policy) => policy switch
    {
        "application_grant" => applications,
        "venue_principal" => applications.Where(application => application.VenueTenantId == tenantId),
        "artist_principal" => applications.Where(application => application.ArtistTenantId == tenantId),
        _ => applications.Where(_ => false),
    };
    public static IQueryable<ApplicationAccessGrant> Eligible(
        IQueryable<ApplicationAccessGrant> grants,
        MembershipSnapshot actor,
        TenantPermission permission,
        ApplicationAccessScope scope,
        DateTime now)
    {
        var audience = actor.AudienceFor(permission);
        return grants.Where(grant =>
            grant.TenantId == actor.TenantId
            && grant.Scope == scope
            && grant.RevokedAt == null
            && grant.ValidFrom <= now
            && (grant.ValidUntil == null || now < grant.ValidUntil)
            && (audience == ResourceAudience.TenantResources
                    && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                || audience == ResourceAudience.AssignedResources
                    && grant.MembershipId == actor.MembershipId));
    }
}