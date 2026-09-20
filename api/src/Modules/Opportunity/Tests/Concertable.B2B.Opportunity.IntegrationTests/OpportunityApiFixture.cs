using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Opportunity.IntegrationTests;

public sealed class OpportunityApiFixture : ApiFixture
{
    private IOpportunityReadDbContext dbContext = null!;
    internal ConcurrencyConflictInterceptor Conflicts { get; } = new();

    internal IQueryable<OpportunityEntity> Opportunities => dbContext.Opportunities;

    internal void ArmOpportunitySave(Func<Task> competingChange) =>
        Conflicts.ArmOnce<OpportunityEntity>(competingChange);

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.AddResettables(Conflicts);
        services.ConfigureDbContext<OpportunityPrivilegedDbContext>(
            (_, options) => options.AddInterceptors(Conflicts));
    }

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<IOpportunityReadDbContext>();
    }
}
