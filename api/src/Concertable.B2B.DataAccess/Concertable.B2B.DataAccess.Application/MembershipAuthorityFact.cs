namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// The narrow authority relation the Tenant module owns and every resource module may read — who belongs to
/// which tenant, at which authority revision. Each module maps it read-only so an access predicate can prove
/// the membership is still present at the revision the request resolved, in the same statement that reads the
/// resource. A membership removed or downgraded since resolution therefore denies the read rather than
/// serving it from a stale decision.
/// <para>
/// It is deliberately not a navigation into Tenant's aggregate: no module writes it, and nothing beyond these
/// three columns is exposed.
/// </para>
/// </summary>
public sealed class MembershipAuthorityFact
{
    private MembershipAuthorityFact() { }

    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public long AuthorizationVersion { get; private set; }
}
