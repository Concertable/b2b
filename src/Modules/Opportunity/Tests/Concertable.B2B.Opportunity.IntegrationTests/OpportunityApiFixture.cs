using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Opportunity.IntegrationTests;

public sealed class OpportunityApiFixture : ApiFixture
{
    private IOpportunityReadDbContext dbContext = null!;

    internal IQueryable<OpportunityEntity> Opportunities => dbContext.Opportunities;

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<IOpportunityReadDbContext>();
    }
}
