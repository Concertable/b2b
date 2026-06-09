using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Domain.Entities;
using Concertable.B2B.Contract.Infrastructure.Data;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Contract.Infrastructure.Repositories;

internal sealed class ContractRepository
    : TenantScopedRepository<ContractEntity>, IContractRepository
{
    public ContractRepository(ContractDbContext context, ITenantContext tenant)
        : base(context, tenant) { }

    public async Task<IEnumerable<ContractEntity>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default) =>
        await context.Contracts
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(ct);
}
