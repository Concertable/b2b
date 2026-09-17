using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.DataAccess.Infrastructure;

// Design-time only builds the model; no query ever evaluates the access filter, and nothing here is authority.
public sealed class DesignTimeResourceAccessContext : IResourceAccessContext
{
    public static readonly DesignTimeResourceAccessContext Instance = new();

    public MembershipSnapshot? Membership => null;

    public DateTime UtcNow => DateTime.UnixEpoch;

    public ResourceAudience AudienceFor(string permission) => ResourceAudience.None;
}
