using System.Linq.Expressions;
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

    // Splices bodies rather than invoking: an Expression.Invoke inside a query filter does not translate.
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
    {
        var body = new ParameterReplacer(right.Parameters[0], left.Parameters[0]).Visit(right.Body)!;
        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, body), left.Parameters);
    }

    private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == source ? target : base.VisitParameter(node);
    }
}
