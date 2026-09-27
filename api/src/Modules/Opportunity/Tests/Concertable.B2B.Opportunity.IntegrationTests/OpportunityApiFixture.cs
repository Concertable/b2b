using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.B2B.Opportunity.Domain.Entities;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Concertable.B2B.Opportunity.IntegrationTests;

public sealed class OpportunityApiFixture : ApiFixture
{
    private IOpportunityReadDbContext dbContext = null!;
    internal OpportunityLifecycleRaceInterceptor LifecycleRace { get; } = new();

    internal IQueryable<OpportunityEntity> Opportunities => dbContext.Opportunities;

    internal void ArmOpportunitySave(Func<Task> competingChange) =>
        LifecycleRace.ArmOnce(competingChange);

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.AddResettables(LifecycleRace);
        services.ConfigureDbContext<OpportunityPrivilegedDbContext>(
            (_, options) => options.AddInterceptors(LifecycleRace));
    }

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<IOpportunityReadDbContext>();
        LifecycleRace.UseDataSource(scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>());
    }
}
