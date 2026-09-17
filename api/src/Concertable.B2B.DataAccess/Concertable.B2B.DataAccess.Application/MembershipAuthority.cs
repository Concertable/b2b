namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// The narrow authority relation the Tenant module owns and every resource module may read — which membership
/// incarnation belongs to which tenant, at which permission revision. Mapped keyless over Tenant's view so an
/// access predicate can prove the membership is still present at the revision the request resolved, in the
/// same statement that reads the resource, and so nothing can track or write it.
/// </summary>
public sealed class MembershipAuthority
{
    public Guid MembershipId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public long PermissionVersion { get; private set; }
}
