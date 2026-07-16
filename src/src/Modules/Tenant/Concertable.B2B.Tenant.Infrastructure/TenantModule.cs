namespace Concertable.B2B.Tenant.Infrastructure;

internal sealed class TenantModule : ITenantModule
{
    private readonly ITenantService service;

    public TenantModule(ITenantService service)
    {
        this.service = service;
    }

    public Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        service.GetByIdAsync(id, ct);

    public Task<IReadOnlyList<MembershipDto>> GetMembershipsAsync(Guid userId, CancellationToken ct = default) =>
        service.GetMembershipsAsync(userId, ct);

    public Task<bool> IsDac7CompleteAsync(Guid tenantId, CancellationToken ct = default) =>
        service.IsDac7CompleteAsync(tenantId, ct);
}
