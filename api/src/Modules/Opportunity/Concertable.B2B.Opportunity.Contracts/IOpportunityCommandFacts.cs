namespace Concertable.B2B.Opportunity.Contracts;

public interface IOpportunityCommandFacts
{
    Task<OpportunityDto?> GetByIdAsync(int opportunityId, CancellationToken ct = default);
}
