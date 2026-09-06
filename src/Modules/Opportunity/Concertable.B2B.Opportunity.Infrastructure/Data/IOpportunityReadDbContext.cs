namespace Concertable.B2B.Opportunity.Infrastructure.Data;

internal interface IOpportunityReadDbContext
{
    IQueryable<OpportunityEntity> Opportunities { get; }
}
