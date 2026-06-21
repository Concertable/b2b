namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// The active membership for the current B2B request — the authority source for permission checks. A
/// B2B-only contract implemented by the Tenant module's request-scoped context; deliberately kept off the
/// shared <c>ICurrentUser</c> in Kernel, which stays audience-agnostic. Resolved per request from the DB
/// membership row, never from the token, so role changes and removals take effect on the next request.
/// </summary>
public interface IMembershipContext
{
    /// <summary>The active membership's role; <see langword="null"/> when the caller has no membership in the active tenant.</summary>
    TenantRole? Role { get; }

    /// <summary>
    /// True iff the active tenant's persona catalog grants <paramref name="permission"/> to the active role
    /// and — when <paramref name="requiredPersona"/> is supplied (a controller's surface persona) — the active
    /// tenant's type matches it. A persona-exclusive permission is unreachable for the other persona by
    /// construction (its catalog doesn't contain it). No active membership ⇒ always
    /// <see langword="false"/> (fails closed).
    /// </summary>
    bool HasPermission(string permission, TenantType? requiredPersona = null);
}
