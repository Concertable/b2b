using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Deal.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Deal.Infrastructure.Repositories;

internal sealed class DealCommandFacts(
    DealPrivilegedDbContext context,
    IDealMapper mapper,
    CommandTransactionAccessor transactions) : IDealCommandFacts
{
    public async Task<DealDto?> GetByIdAsync(int dealId, CancellationToken ct = default)
    {
        await (transactions.Current
            ?? throw new InvalidOperationException("Deal command facts require an active command transaction."))
            .EnlistAsync(context, ct);

        var deal = await context.Deals.SingleOrDefaultAsync(candidate => candidate.Id == dealId, ct);
        return deal is null ? null : mapper.ToDeal(deal);
    }
}
