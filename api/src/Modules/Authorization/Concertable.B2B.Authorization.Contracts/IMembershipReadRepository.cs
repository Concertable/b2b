namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// The port Authorization reads memberships through, implemented by the module that owns membership storage.
/// Authorization depends on this contract and never on that module's assemblies; the composition root binds
/// the two, which is what keeps the dependency acyclic.
/// </summary>
public interface IMembershipReadRepository
{
    Task<MembershipSnapshot?> GetSnapshotByUserIdAndTenantIdAsync(
        Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipSnapshot>> GetSnapshotsByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
