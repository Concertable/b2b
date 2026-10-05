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
    internal RaceInterceptor Race { get; } = new();

    internal IQueryable<OpportunityEntity> Opportunities => dbContext.Opportunities;

    internal void ArmSave(Func<Task> competingChange) =>
        Race.ArmOnce(competingChange);

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.AddResettables(Race);
        services.ConfigureDbContext<OpportunityPrivilegedDbContext>(
            (_, options) => options.AddInterceptors(Race));
    }

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<IOpportunityReadDbContext>();
        Race.UseDataSource(scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>());
    }
}
