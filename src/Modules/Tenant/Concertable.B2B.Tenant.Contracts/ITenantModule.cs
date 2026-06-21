namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantModule
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The caller's memberships — feeds the <c>/api/auth/me</c> tenant switcher payload.</summary>
    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);
}
