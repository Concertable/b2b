using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Repositories;

internal sealed class DealPrivilegedReadRepository(
    DealPrivilegedDbContext context,
    IDealMapper mapper,
    CommandTransactionAccessor transactions) : IDealPrivilegedReadRepository
{
    public async Task<DealDto?> GetByIdAsync(int dealId, CancellationToken ct = default)
    {
        await (transactions.Current
            ?? throw new InvalidOperationException("Deal privileged queries require an active transaction."))
            .EnlistAsync(context, ct);

        var deal = await context.Deals.SingleOrDefaultAsync(candidate => candidate.Id == dealId, ct);
        return deal is null ? null : mapper.ToDeal(deal);
    }
}
