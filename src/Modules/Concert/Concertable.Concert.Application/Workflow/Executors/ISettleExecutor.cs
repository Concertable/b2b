namespace Concertable.Concert.Application.Workflow.Executors;

internal interface ISettleExecutor
{
    Task ExecuteAsync(int bookingId);
}
