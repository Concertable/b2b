using System.Linq.Expressions;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Data;

internal static class ConcertAuthorizationPolicy
{
    public static Expression<Func<ConcertEntity, bool>> Concerts(
        ConcertPrivilegedDbContext context,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        ResourceAuthorizationSnapshot? pinned = null,
        bool requireCurrentMembership = true)
    {
        if (binding.Resource != ResourceKind.Concert
            || actor.AudienceFor(binding.Permission) == ResourceAudience.None
            || pinned is not null && (pinned.Grants.Length != binding.RequiredScopes.Length
                || pinned.PrincipalTenantId != PrincipalTenantId(binding.Policy, actor)))
            return concert => false;

        Expression<Func<ConcertEntity, bool>> policy = binding.Policy switch
        {
            "concert_grant" => concert => true,
            "venue_principal" => concert => concert.VenueTenantId == actor.TenantId,
            "artist_principal" => concert => concert.ArtistTenantId == actor.TenantId,
            "either_principal" or "principal_administration" =>
                concert => concert.VenueTenantId == actor.TenantId
                    || concert.ArtistTenantId == actor.TenantId,
            _ => concert => false,
        };

        if (requireCurrentMembership)
        {
            policy = And(policy, concert => context.MembershipAuthority.Any(member =>
                member.MembershipId == actor.MembershipId
                && member.TenantId == actor.TenantId
                && member.UserId == actor.UserId
                && member.PermissionVersion == actor.PermissionVersion
                && member.RolePolicyVersion == actor.RolePolicyVersion));
        }

        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<ConcertAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                return concert => false;

            var original = pinned?.Grants.SingleOrDefault(grant => grant.Scope == name);
            if (pinned is not null && original is null)
                return concert => false;

            var grants = ConcertGrants(context, actor, binding.Permission, scope, now, original);
            policy = And(policy, concert => grants.Any(grant => grant.ResourceId == concert.Id));
        }

        return policy;
    }

    public static Expression<Func<InvoiceEntity, bool>> Invoices(
        ConcertPrivilegedDbContext context,
        ResourcePolicyBinding binding,
        MembershipSnapshot actor,
        DateTimeOffset now,
        ResourceAuthorizationSnapshot? pinned = null,
        bool requireCurrentMembership = true)
    {
        if (binding.Resource != ResourceKind.Invoice
            || binding.Policy != "invoice_grant"
            || actor.AudienceFor(binding.Permission) == ResourceAudience.None
            || pinned is not null && (pinned.PrincipalTenantId is not null
                || pinned.Grants.Length != binding.RequiredScopes.Length))
            return invoice => false;

        Expression<Func<InvoiceEntity, bool>> policy = invoice => true;
        if (requireCurrentMembership)
        {
            policy = And(policy, invoice => context.MembershipAuthority.Any(member =>
                member.MembershipId == actor.MembershipId
                && member.TenantId == actor.TenantId
                && member.UserId == actor.UserId
                && member.PermissionVersion == actor.PermissionVersion
                && member.RolePolicyVersion == actor.RolePolicyVersion));
        }

        foreach (var name in binding.RequiredScopes)
        {
            if (!Enum.TryParse<InvoiceAccessScope>(name, false, out var scope)
                || !Enum.IsDefined(scope))
                return invoice => false;

            var original = pinned?.Grants.SingleOrDefault(grant => grant.Scope == name);
            if (pinned is not null && original is null)
                return invoice => false;

            var grants = InvoiceGrants(context, actor, binding.Permission, scope, now, original);
            policy = And(policy, invoice => grants.Any(grant => grant.ResourceId == invoice.Id));
        }

        return policy;
    }

    public static IQueryable<ConcertAccessGrant> ConcertGrants(
        ConcertPrivilegedDbContext context,
        MembershipSnapshot actor,
        TenantPermission permission,
        ConcertAccessScope scope,
        DateTimeOffset now,
        ResourceGrantSnapshot? pinned = null)
    {
        var audience = actor.AudienceFor(permission);
        var at = now.UtcDateTime;
        var grants = context.ConcertAccessGrants.AsNoTracking().Where(grant =>
            grant.Scope == scope
            && grant.TenantId == actor.TenantId
            && grant.RevokedAt == null
            && grant.ValidFrom <= at
            && (grant.ValidUntil == null || grant.ValidUntil > at)
            && (audience == ResourceAudience.TenantResources
                    && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                || audience == ResourceAudience.AssignedResources
                    && grant.MembershipId == actor.MembershipId));

        if (pinned is not null)
        {
            var until = pinned.ValidUntil?.UtcDateTime;
            var from = pinned.ValidFrom.UtcDateTime;
            grants = grants.Where(grant =>
                grant.Id == pinned.GrantId
                && grant.Version == pinned.Version
                && grant.ValidFrom == from
                && grant.ValidUntil == until);
        }

        return grants;
    }

    public static IQueryable<InvoiceAccessGrant> InvoiceGrants(
        ConcertPrivilegedDbContext context,
        MembershipSnapshot actor,
        TenantPermission permission,
        InvoiceAccessScope scope,
        DateTimeOffset now,
        ResourceGrantSnapshot? pinned = null)
    {
        var audience = actor.AudienceFor(permission);
        var at = now.UtcDateTime;
        var grants = context.InvoiceAccessGrants.AsNoTracking().Where(grant =>
            grant.Scope == scope
            && grant.TenantId == actor.TenantId
            && grant.RevokedAt == null
            && grant.ValidFrom <= at
            && (grant.ValidUntil == null || grant.ValidUntil > at)
            && (audience == ResourceAudience.TenantResources
                    && (grant.MembershipId == null || grant.MembershipId == actor.MembershipId)
                || audience == ResourceAudience.AssignedResources
                    && grant.MembershipId == actor.MembershipId));

        if (pinned is not null)
        {
            var until = pinned.ValidUntil?.UtcDateTime;
            var from = pinned.ValidFrom.UtcDateTime;
            grants = grants.Where(grant =>
                grant.Id == pinned.GrantId
                && grant.Version == pinned.Version
                && grant.ValidFrom == from
                && grant.ValidUntil == until);
        }

        return grants;
    }

    public static Guid? PrincipalTenantId(string policy, MembershipSnapshot actor) =>
        policy is "venue_principal" or "artist_principal" or "either_principal"
            or "principal_administration" ? actor.TenantId : null;

    private static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var body = new ParameterReplacer(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, body), left.Parameters);
    }

    private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
