namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IApplicationNotifier
{
    Task AppliedAsync(int applicationId);
    Task AcceptedAsync(int applicationId);
    Task WithdrawnAsync(int applicationId);
    Task RejectedAsync(int applicationId);
    Task CancelledAsync(int applicationId);
}
