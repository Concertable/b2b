namespace Concertable.B2B.Tenant.Contracts;

public interface ITenantModule
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>The caller's memberships — feeds the <c>/api/auth/me</c> tenant switcher payload.</summary>
    Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Whether the tenant holds a complete, jurisdiction-valid DAC7 seller tax identity — the single
    /// source of truth (<c>IDac7Strategy.IsComplete</c>) the fail-closed payout gate and the dashboard nag both
    /// consume. Fail-closed: a missing tenant or absent/invalid compliance is not complete.</summary>
    Task<bool> IsDac7CompleteAsync(Guid tenantId, CancellationToken ct = default);
}
