using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.IntegrationTests.Concert;

internal static class ConcertWorkflowExtensions
{
    public static async Task FinishConcertAsync(this ConcertApiFixture fixture, int concertId)
    {
        using var scope = fixture.Services.CreateScope();
        var concertWorkflowModule = scope.ServiceProvider.GetRequiredService<IConcertWorkflowModule>();
        await concertWorkflowModule.FinishAsync(concertId);
    }

    public static async Task DeclareDoorRevenueAsync(this ConcertApiFixture fixture, int concertId, decimal doorRevenue)
    {
        using var scope = fixture.Services.CreateScope();
        var concertWorkflowModule = scope.ServiceProvider.GetRequiredService<IConcertWorkflowModule>();
        await concertWorkflowModule.DeclareDoorRevenueAsync(concertId, doorRevenue);
    }

    public static async Task RunCompletionAsync(this ConcertApiFixture fixture)
    {
        using var scope = fixture.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IConcertCompletionRunner>();
        await runner.RunAsync();
    }
}
