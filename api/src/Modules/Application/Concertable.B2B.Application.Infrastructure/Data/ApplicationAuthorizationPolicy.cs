using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal static class ApplicationAuthorizationPolicy
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
    public static IQueryable<ApplicationEntity> Visible(
        IQueryable<ApplicationEntity> applications,
        IQueryable<ApplicationAccessGrant> grants,
        IQueryable<MembershipAuthority> members,
        MembershipSnapshot actor, ResourcePolicyBinding binding, DateTime now)
    {
        var visible = EligibleApplications(applications, actor.TenantId, binding.Policy)
            .Where(application => members.Any(member =>
                member.MembershipId == actor.MembershipId
                && member.TenantId == actor.TenantId
                && member.UserId == actor.UserId
                && member.PermissionVersion == actor.PermissionVersion
                && member.RolePolicyVersion == actor.RolePolicyVersion));
        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<ApplicationAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                throw new InvalidOperationException("An invalid application scope was registered.");
            var eligible = Eligible(grants, actor, binding.Permission, scope, now);
            visible = visible.Where(application => eligible.Any(grant => grant.ResourceId == application.Id));
        }
        return visible;
    }

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