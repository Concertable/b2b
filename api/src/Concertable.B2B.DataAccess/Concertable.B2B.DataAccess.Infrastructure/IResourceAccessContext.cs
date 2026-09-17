using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// Everything an access predicate needs about the caller, in one request-scoped value: which membership
/// incarnation is acting, how wide each of its permissions reaches, and the instant validity is judged at.
/// There is no trusted-stance flag: the absence of a human is not authority, so system work uses a privileged
/// context rather than a wider reading of this one.
/// </summary>
public interface IResourceAccessContext
{
    MembershipSnapshot? Membership { get; }

    DateTime UtcNow { get; }

    ResourceAudience AudienceFor(string permission);
}
