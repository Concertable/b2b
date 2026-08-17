using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Deal.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Repositories;

internal sealed class DealRepository
    : TenantScopedRepository<DealEntity>, IDealRepository
{
    private readonly DealDbContext context;

    public DealRepository(DealDbContext context, ITenantContext tenant)
        : base(context, tenant)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<DealEntity>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default) =>
        await context.Deals
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);
}
