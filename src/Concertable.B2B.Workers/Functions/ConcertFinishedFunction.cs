using Concertable.B2B.Concert.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;

namespace Concertable.B2B.Workers.Functions;

internal sealed class ConcertFinishedFunction(ICompletionRunner runner)
{
    [Function(nameof(ConcertFinishedFunction))]
    public Task Run([TimerTrigger("0 0 * * * *")] TimerInfo timer) => runner.RunAsync();
}
