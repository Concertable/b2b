namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Declares which tenant a caller acts as, for writes that have no HTTP request to resolve one from — a
/// seeder, a worker, an outbox dispatcher. The tenant write guard refuses a tenant-scoped write with no
/// current tenant, so a background writer states its tenant rather than bypassing the guard.
/// </summary>
public interface ITenantScope
{
    /// <summary>Acts as <paramref name="tenantId"/> until the returned scope is disposed.</summary>
    IDisposable As(Guid tenantId);
}
