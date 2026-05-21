using Concertable.Concert.Application.Workflow.Steps;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Steps;

internal class NoOpSettleStep : ISettleStep
{
    public Task ExecuteAsync(int bookingId) => Task.CompletedTask;
}
