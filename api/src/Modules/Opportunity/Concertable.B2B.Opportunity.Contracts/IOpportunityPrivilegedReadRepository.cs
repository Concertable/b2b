namespace Concertable.B2B.Opportunity.Contracts;

public interface IOpportunityPrivilegedReadRepository
{
    Task<OpportunityDto?> GetByIdAsync(int opportunityId, CancellationToken ct = default);
}
