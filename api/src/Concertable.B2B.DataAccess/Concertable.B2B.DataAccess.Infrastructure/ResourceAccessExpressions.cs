using System.Linq.Expressions;
using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The membership, validity and time restrictions every grant family shares, and the combinator a module uses
/// to add its own scope/audience policy to them. Bodies are spliced rather than invoked: an
/// <c>Expression.Invoke</c> or a compiled delegate inside a query filter would not translate, and the whole
/// point is that these restrictions reach SQL on the same row as the scope check.
/// </summary>
public static class ResourceAccessExpressions
{
    /// <summary>
    /// A grant row addressed to the acting membership's tenant, whose membership incarnation is still present
    /// at the resolved permission revision, and which is live at the instant this query runs.
    /// </summary>
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
