namespace Concertable.B2B.Authorization.Contracts;

public interface IMembershipAuthorityFence
{
    Task<MembershipSnapshot?> RequireCurrentAsync(
        MembershipSnapshot expected,
        CancellationToken ct = default);
}
