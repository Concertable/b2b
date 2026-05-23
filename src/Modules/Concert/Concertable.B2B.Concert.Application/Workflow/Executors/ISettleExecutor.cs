namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface ISettleExecutor
{
    Task ExecuteAsync(int bookingId);
}
