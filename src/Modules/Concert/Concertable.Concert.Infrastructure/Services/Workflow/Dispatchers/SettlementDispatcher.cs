using Concertable.Concert.Application.Workflow.Executors;

namespace Concertable.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class SettlementDispatcher : ISettlementDispatcher
{
    private readonly ISettleExecutor executor;

    public SettlementDispatcher(ISettleExecutor executor)
    {
        this.executor = executor;
    }

    public Task SettleAsync(int bookingId) => executor.ExecuteAsync(bookingId);
}
