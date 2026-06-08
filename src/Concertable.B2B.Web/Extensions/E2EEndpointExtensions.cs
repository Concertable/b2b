using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.Kernel;

namespace Concertable.B2B.Web.Extensions;

public static class E2EEndpointExtensions
{
    public static void MapE2EEndpoints(this WebApplication app)
    {
        app.MapPost("/e2e/finish/{concertId:int}", async (int concertId, ICompletionDispatcher CompletionDispatcher) =>
        {
            var result = await CompletionDispatcher.FinishAsync(concertId);
            return result.IsFailed
                ? Results.BadRequest(result.Errors.SelectMessages())
                : Results.Ok();
        });

        app.MapPost("/e2e/run-completion", async (IConcertCompletionRunner runner) =>
        {
            await runner.RunAsync();
            return Results.Ok();
        });
    }
}
