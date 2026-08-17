using Concertable.B2B.Opportunity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Opportunity.Infrastructure;

internal sealed class OpportunityModule : IOpportunityModule
{
    private readonly IOpportunityReadDbContext readContext;
    private readonly IOpportunityHandoffRepository handoffRepository;

    public OpportunityModule(
        IOpportunityReadDbContext readContext,
        IOpportunityHandoffRepository handoffRepository)
    {
        this.readContext = readContext;
        this.handoffRepository = handoffRepository;
    }

    public async Task<Option<OpportunityDetails>> GetDetailsAsync(
        int opportunityId,
        CancellationToken ct = default)
    {
        var details = await readContext.Opportunities
            .Where(opportunity => opportunity.Id == opportunityId)
            .Select(opportunity => new OpportunityDetails(
                opportunity.Id,
                opportunity.VenueId,
                opportunity.TenantId,
                opportunity.DealId,
                opportunity.Period.Start,
                opportunity.Period.End,
                opportunity.Genres))
            .FirstOrDefaultAsync(ct);

        return details is null ? Option.None<OpportunityDetails>() : Option.Some(details);
    }

    public async Task MarkFilledAsync(
        int opportunityId,
        Guid venueTenantId,
        CancellationToken ct = default)
    {
        var opportunity = await handoffRepository.GetByIdAsync(opportunityId, ct)
            ?? throw new InvalidOperationException($"Opportunity {opportunityId} was not found.");

        if (opportunity.TenantId != venueTenantId)
            throw new InvalidOperationException($"Opportunity {opportunityId} is not owned by tenant {venueTenantId}.");

        opportunity.MarkFilled();
        await handoffRepository.SaveChangesAsync(ct);
    }
}
