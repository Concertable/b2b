using Concertable.B2B.Concert.Application.Workflow.Executors;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;

internal sealed class EscrowDispatcher : IEscrowDispatcher
{
    private readonly IEscrowExecutor executor;

    public EscrowDispatcher(IEscrowExecutor executor)
    {
        this.executor = executor;
    }

    public Task SucceededAsync(int bookingId) => executor.ExecuteAsync(bookingId);

    public Task FailedAsync(int bookingId) => executor.ExecuteFailedAsync(bookingId);
}
