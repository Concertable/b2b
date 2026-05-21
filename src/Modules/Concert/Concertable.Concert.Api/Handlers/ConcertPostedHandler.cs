using Concertable.Shared;

namespace Concertable.Concert.Api.Handlers;

internal class ConcertPostedHandler : IConcertPostedHandler
{
    private readonly IBackgroundTaskRunner taskRunner;
    private readonly IConcertNotifier notifier;

    public ConcertPostedHandler(IBackgroundTaskRunner taskRunner, IConcertNotifier notifier)
    {
        this.taskRunner = taskRunner;
        this.notifier = notifier;
    }

    public async Task HandleAsync(ConcertPostResponse result)
    {
        await taskRunner.RunAsync(async ct =>
        {
            var tasks = result.UserIds.Select(userId =>
                notifier.ConcertPostedAsync(userId.ToString(), result.ConcertHeader));

            await Task.WhenAll(tasks);
        });
    }
}
