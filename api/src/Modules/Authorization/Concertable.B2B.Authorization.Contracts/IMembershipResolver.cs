using Reunion;

namespace Concertable.B2B.Authorization.Contracts;

public interface IMembershipResolver
{
    Task<Option<MembershipSnapshot>> ResolveSnapshotAsync(
        MembershipSnapshot expected,
        CancellationToken ct = default);
}
