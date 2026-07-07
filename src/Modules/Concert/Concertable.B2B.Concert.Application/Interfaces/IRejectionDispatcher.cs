namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IRejectionDispatcher
{
    Task RejectAsync(int applicationId);
}
