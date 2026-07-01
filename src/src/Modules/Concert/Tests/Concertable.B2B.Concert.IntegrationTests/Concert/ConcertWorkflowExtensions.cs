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
}
