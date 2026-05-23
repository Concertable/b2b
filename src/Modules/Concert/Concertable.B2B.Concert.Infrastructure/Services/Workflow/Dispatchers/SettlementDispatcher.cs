using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal class SettlementDispatcher : ISettlementDispatcher
{
    private readonly ISettleExecutor executor;

    public SettlementDispatcher(ISettleExecutor executor)
    {
        this.executor = executor;
    }

    public Task SettleAsync(int bookingId) => executor.ExecuteAsync(bookingId);
}
