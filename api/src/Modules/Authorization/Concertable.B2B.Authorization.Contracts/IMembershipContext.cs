namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// The active membership for the current B2B request — the authority source for permission checks. A
/// B2B-only contract implemented by the Authorization module's request-scoped context; deliberately kept off
/// the shared <c>ICurrentUser</c> in Kernel, which stays audience-agnostic. Resolved per request from the
/// membership row, never from the token, so role changes and removals take effect on the next request.
/// </summary>
public interface IMembershipContext
{
    MembershipSnapshot? Membership { get; }

    bool HasPermission(string permission);

    ResourceAudience AudienceFor(string permission);
}
