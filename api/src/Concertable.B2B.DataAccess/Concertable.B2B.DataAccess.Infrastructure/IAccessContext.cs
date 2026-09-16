namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// Everything an access predicate needs about the caller, in one request-scoped value: who is acting, in
/// which tenant, at which authority revision, whether a trusted execution stance was explicitly established,
/// and the instant validity is judged at.
/// </summary>
public interface IAccessContext
{
    Guid? TenantId { get; }
    Guid? UserId { get; }
    long? AuthorizationVersion { get; }

    /// <summary>True only where a caller explicitly entered a trusted execution scope. The absence of a
    /// request is not authority, so an unestablished caller reads nothing.</summary>
    bool IsHost { get; }

    DateTime UtcNow { get; }
}
