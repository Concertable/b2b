using Concertable.Contract.Application.Interfaces;
using Concertable.Contract.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Contract.Infrastructure.Repositories;

internal class ContractRepository(ContractDbContext context)
    : Repository<ContractEntity>(context), IContractRepository
{
    public async Task<IEnumerable<ContractEntity>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default) =>
        await context.Contracts.Where(c => ids.Contains(c.Id)).ToListAsync(ct);
}
