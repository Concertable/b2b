namespace Concertable.B2B.Opportunity.Contracts;

public interface IOpportunityPrivilegedRepository
{
    Task<OpportunityDto?> GetByIdAsync(int opportunityId, CancellationToken ct = default);
}
