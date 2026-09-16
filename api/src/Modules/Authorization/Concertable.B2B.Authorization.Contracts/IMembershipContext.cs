namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// The active membership for the current B2B request — the authority source for permission checks. A
/// B2B-only contract implemented by the Authorization module's request-scoped context; deliberately kept off
/// the shared <c>ICurrentUser</c> in Kernel, which stays audience-agnostic. Resolved per request from the
/// membership row, never from the token, so role changes and removals take effect on the next request.
/// </summary>
public interface IMembershipContext
{
    /// <summary>The active membership's role; <see langword="null"/> when the caller has no membership in the active tenant.</summary>
    TenantRole? Role { get; }

    /// <summary>The authenticated human acting in the tenant; <see langword="null"/> outside an authenticated request.</summary>
    Guid? UserId { get; }

    /// <summary>
    /// The membership's authority revision at resolution time. A resource read re-checks the membership at
    /// this revision inside its own authorisation query, so a removal or downgrade between resolution and
    /// query denies rather than serves.
    /// </summary>
    long? AuthorizationVersion { get; }

    bool HasPermission(string permission);
}
