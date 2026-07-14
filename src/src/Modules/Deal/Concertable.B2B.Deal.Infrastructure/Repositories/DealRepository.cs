using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Domain.Entities;
using Concertable.B2B.Deal.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Repositories;

internal sealed class DealRepository
    : TenantScopedRepository<DealEntity>, IDealRepository
{
    public DealRepository(DealDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<IEnumerable<DealEntity>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default) =>
        await context.Deals
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);
}
