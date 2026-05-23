using Concertable.B2B.Concert.Application.Workflow.Steps;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal class NoOpSettleStep : ISettleStep
{
    public Task ExecuteAsync(int bookingId) => Task.CompletedTask;
}
