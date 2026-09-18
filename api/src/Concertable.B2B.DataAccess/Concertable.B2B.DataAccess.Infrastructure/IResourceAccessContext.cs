using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Infrastructure;

public interface IResourceAccessContext
{
    MembershipSnapshot? Membership { get; }

    DateTime UtcNow { get; }

    ResourceAudience AudienceFor(string permission);
}
