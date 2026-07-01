namespace Concertable.B2B.Concert.Application.Workflow.Executors;

internal interface IEscrowExecutor
{
    Task ExecuteAsync(int bookingId);
    Task ExecuteFailedAsync(int bookingId);
}
