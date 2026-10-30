using System.Linq.Expressions;
using Concertable.B2B.Authorization.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

public abstract class ResourceAccessGrant<TScope> : IGuidEntity
    where TScope : struct, Enum
{
    protected ResourceAccessGrant() { }

    public Guid Id { get; protected set; }

    public int ResourceId { get; protected set; }

    public Guid TenantId { get; protected set; }

    public Guid? MembershipId { get; protected set; }

    public TScope Scope { get; protected set; }
    public DateTime ValidFrom { get; protected set; }
    public DateTime? ValidUntil { get; protected set; }
    public DateTime? RevokedAt { get; protected set; }
    public Guid IssuedByTenantId { get; protected set; }

    public Guid? IssuedByUserId { get; protected set; }

    public ResourceGrantKind Kind { get; protected set; }
    public long Version { get; protected set; }

    public bool IsLiveAt(DateTime at) =>
        RevokedAt is null && ValidFrom <= at && (ValidUntil is null || ValidUntil > at);

    public static Expression<Func<TGrant, bool>> IsLiveFor<TGrant>(
        MembershipSnapshot actor,
        TenantPermission permission,
        DateTime at,
        params TScope[] scopes)
        where TGrant : ResourceAccessGrant<TScope>
    {
        var audience = actor.AudienceFor(permission);
        return IsLiveFor<TGrant>(() => actor.TenantId, () => actor.MembershipId, () => audience, () => at, scopes);
    }

    public static Expression<Func<TGrant, bool>> IsLiveFor<TGrant>(
        Expression<Func<Guid?>> tenantId,
        Expression<Func<Guid?>> membershipId,
        Expression<Func<ResourceAudience>> audience,
        Expression<Func<DateTime>> at,
        params TScope[] scopes)
        where TGrant : ResourceAccessGrant<TScope>
    {
        if (scopes.Length == 0)
            throw new ArgumentException("At least one scope is required.", nameof(scopes));

        Expression<Func<TGrant, Guid?, Guid?, ResourceAudience, DateTime, bool>> live =
            (grant, grantee, member, coverage, now) =>
                grant.TenantId == grantee
                && grant.RevokedAt == null
                && grant.ValidFrom <= now
                && (grant.ValidUntil == null || now < grant.ValidUntil)
                && (coverage == ResourceAudience.TenantResources
                        && (grant.MembershipId == null || grant.MembershipId == member)
                    || coverage == ResourceAudience.AssignedResources
                        && grant.MembershipId == member);
        var grant = live.Parameters[0];
        var body = new ParameterBinder(new Dictionary<ParameterExpression, Expression>
        {
            [live.Parameters[1]] = tenantId.Body,
            [live.Parameters[2]] = membershipId.Body,
            [live.Parameters[3]] = audience.Body,
            [live.Parameters[4]] = at.Body,
        }).Visit(live.Body)!;
        var scope = Expression.Property(grant, nameof(Scope));
        var scopeMatches = scopes
            .Select(candidate => (Expression)Expression.Equal(scope, Expression.Constant(candidate)))
            .Aggregate(Expression.OrElse);

        return Expression.Lambda<Func<TGrant, bool>>(Expression.AndAlso(scopeMatches, body), grant);
    }

    protected void Initialize(
        int resourceId,
        Guid tenantId,
        Guid? membershipId,
        TScope scope,
        Guid issuedByTenantId,
        Guid? issuedByUserId,
        ResourceGrantKind kind,
        DateTime at,
        DateTime? validUntil)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A grant requires the tenant it discloses to.", nameof(tenantId));

        Id = Guid.NewGuid();
        ResourceId = resourceId;
        TenantId = tenantId;
        MembershipId = membershipId;
        Scope = scope;
        ValidFrom = at;
        ValidUntil = validUntil;
        IssuedByTenantId = issuedByTenantId;
        IssuedByUserId = issuedByUserId;
        Kind = kind;
        Version = 1;
    }

    protected void RevokeCore(DateTime at)
    {
        if (RevokedAt is not null)
            return;

        RevokedAt = at;
        Version++;
    }
}

file sealed class ParameterBinder(IReadOnlyDictionary<ParameterExpression, Expression> bindings) : ExpressionVisitor
{
    protected override Expression VisitParameter(ParameterExpression node) =>
        bindings.TryGetValue(node, out var bound) ? bound : base.VisitParameter(node);
}
