using System.Linq.Expressions;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.Infrastructure;

public static class ResourceAccessExpressions
{
    public static Expression<Func<TGrant, bool>> LiveForCurrentMember<TGrant, TScope>(IHasResourceAccessContext context)
        where TGrant : ResourceAccessGrant<TScope>
        where TScope : struct, Enum =>
        grant =>
            context.ActiveMembershipId != null
            && context.MembershipAuthority.Any(member =>
                member.MembershipId == context.ActiveMembershipId
                && member.TenantId == context.ActiveTenantId
                && member.UserId == context.ActiveUserId
                && member.PermissionVersion == context.ActivePermissionVersion)
            && grant.TenantId == context.ActiveTenantId
            && grant.RevokedAt == null
            && grant.ValidFrom <= context.ResourceAccess.UtcNow
            && context.ResourceAccess.UtcNow < (grant.ValidUntil ?? DateTime.MaxValue);

    public static Expression<Func<TGrant, bool>> LiveForAudience<TGrant, TScope>(
        IHasResourceAccessContext context,
        Expression<Func<TGrant, ResourceAudience>> audience,
        params TScope[] scopes)
        where TGrant : ResourceAccessGrant<TScope>
        where TScope : struct, Enum
    {
        if (scopes.Length == 0)
            throw new ArgumentException("At least one scope is required.", nameof(scopes));

        var live = LiveForCurrentMember<TGrant, TScope>(context);
        var parameter = live.Parameters[0];
        var audienceBody = new ParameterReplacer(audience.Parameters[0], parameter).Visit(audience.Body)!;
        var scopeMember = Expression.Property(parameter, nameof(ResourceAccessGrant<TScope>.Scope));
        var scopeBody = scopes
            .Select(scope => Expression.Equal(scopeMember, Expression.Constant(scope)))
            .Aggregate(Expression.OrElse);
        Expression<Func<TGrant, bool>> tenantResources = grant =>
            grant.MembershipId == null || grant.MembershipId == context.ActiveMembershipId;
        Expression<Func<TGrant, bool>> assignedResources = grant =>
            grant.MembershipId == context.ActiveMembershipId;
        var tenantResourcesBody = new ParameterReplacer(tenantResources.Parameters[0], parameter)
            .Visit(tenantResources.Body)!;
        var assignedResourcesBody = new ParameterReplacer(assignedResources.Parameters[0], parameter)
            .Visit(assignedResources.Body)!;
        var audienceFilter = Expression.OrElse(
            Expression.AndAlso(
                Expression.Equal(audienceBody, Expression.Constant(ResourceAudience.TenantResources)),
                tenantResourcesBody),
            Expression.AndAlso(
                Expression.Equal(audienceBody, Expression.Constant(ResourceAudience.AssignedResources)),
                assignedResourcesBody));

        return Expression.Lambda<Func<TGrant, bool>>(
            Expression.AndAlso(live.Body, Expression.AndAlso(scopeBody, audienceFilter)),
            parameter);
    }

    // Splices bodies rather than invoking: an Expression.Invoke inside a query filter does not translate.
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var body = new ParameterReplacer(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, body), left.Parameters);
    }

    private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
