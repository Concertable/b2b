namespace Concertable.B2B.Authorization.Contracts;

public interface IMembershipReadRepository
{
    Task<MembershipSnapshot?> GetSnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
