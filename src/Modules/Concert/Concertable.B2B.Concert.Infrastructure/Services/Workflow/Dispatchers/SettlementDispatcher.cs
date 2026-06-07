using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class SettlementDispatcher : ISettlementDispatcher
{
    private readonly ISettlementExecutor executor;

    public SettlementDispatcher(ISettlementExecutor executor)
    {
        this.executor = executor;
    }

    public Task SucceededAsync(int bookingId) => executor.ExecuteAsync(bookingId);

    public Task FailedAsync(int bookingId) => executor.ExecuteFailedAsync(bookingId);
}
