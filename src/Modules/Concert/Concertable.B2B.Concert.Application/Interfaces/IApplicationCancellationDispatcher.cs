namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationCancellationDispatcher
{
    Task CancelAsync(int applicationId);
}
