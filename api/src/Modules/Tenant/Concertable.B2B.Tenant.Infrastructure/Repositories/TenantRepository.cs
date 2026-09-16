using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantRepository : Repository<TenantEntity>, ITenantRepository
{
    private readonly TenantDbContext context;

    public TenantRepository(TenantDbContext context) : base(context)
    {
        this.context = context;
    }

    public Task<BusinessFacts?> GetBusinessFactsByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Tenants
            .Where(t => t.Id == tenantId)
            .ToBusinessFacts(context.BusinessProfiles)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<BusinessFacts>> GetBusinessFactsByTenantIdsAsync(
        IReadOnlyCollection<Guid> tenantIds,
        CancellationToken ct = default) =>
        await context.Tenants
            .Where(t => tenantIds.Contains(t.Id))
            .ToBusinessFacts(context.BusinessProfiles)
            .ToListAsync(ct);

    public Task<bool> HasActiveBusinessProfileAsync(
        Guid tenantId,
        TenantBusinessProfileKind kind,
        CancellationToken ct = default) =>
        context.BusinessProfiles
            .AnyAsync(p => p.TenantId == tenantId && p.Kind == kind && p.RetiredAt == null, ct);
}
