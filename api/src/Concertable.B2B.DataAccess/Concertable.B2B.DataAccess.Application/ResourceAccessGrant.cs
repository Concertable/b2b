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
