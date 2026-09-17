using Concertable.Kernel;

namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// One tenant's access to one resource, at one scope. Each owning module declares its own grant entity over
/// its own scope vocabulary, because the resource foreign key is real and module-local; this base carries only
/// the audience, validity and provenance every grant family shares.
/// <para>
/// A row with a <see cref="MembershipId"/> narrows the tenant audience to that one membership incarnation, and
/// still requires it to be current. It is sufficient on its own: pairing it with a tenant-wide row would
/// disclose the same resource to every other member.
/// </para>
/// </summary>
public abstract class ResourceAccessGrant<TScope> : IGuidEntity
    where TScope : struct, Enum
{
    protected ResourceAccessGrant() { }

    public Guid Id { get; protected set; }

    /// <summary>The owning module's own key for the resource disclosed.</summary>
    public int ResourceId { get; protected set; }

    /// <summary>The tenant the resource is disclosed to.</summary>
    public Guid TenantId { get; protected set; }

    /// <summary>When set, only this membership of <see cref="TenantId"/> may read the resource.</summary>
    public Guid? MembershipId { get; protected set; }

    public TScope Scope { get; protected set; }
    public DateTime ValidFrom { get; protected set; }
    public DateTime? ValidUntil { get; protected set; }
    public DateTime? RevokedAt { get; protected set; }
    public Guid IssuedByTenantId { get; protected set; }

    /// <summary>The human who issued it, or null where the act had none — a resource created by a payment
    /// confirmation grants its principals access with no person to attribute it to.</summary>
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
