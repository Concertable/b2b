using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Opportunity.Application.Mappers;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure.Repositories;

internal sealed class OpportunityCommandFacts(
    OpportunityPrivilegedDbContext context,
    CommandTransactionAccessor transactions) : IOpportunityCommandFacts
{
    public async Task<OpportunityDto?> GetByIdAsync(int opportunityId, CancellationToken ct = default)
    {
        await (transactions.Current
            ?? throw new InvalidOperationException("Opportunity command facts require an active command transaction."))
            .EnlistAsync(context, ct);

        var opportunity = await context.Opportunities
            .SingleOrDefaultAsync(candidate => candidate.Id == opportunityId, ct);
        return opportunity?.ToDto();
    }
}
