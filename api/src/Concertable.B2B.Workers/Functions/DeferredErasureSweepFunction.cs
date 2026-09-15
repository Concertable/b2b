using Concertable.B2B.Privacy.Application.Interfaces;
using Microsoft.Azure.Functions.Worker;

namespace Concertable.B2B.Workers.Functions;

internal sealed class DeferredErasureSweepFunction(IDeferredErasureRunner runner)
{
    [Function(nameof(DeferredErasureSweepFunction))]
    public Task Run([TimerTrigger("0 30 * * * *")] TimerInfo timer) => runner.RunAsync();
}
