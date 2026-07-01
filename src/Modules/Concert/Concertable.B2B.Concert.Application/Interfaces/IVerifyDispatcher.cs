namespace Concertable.B2B.Concert.Application.Interfaces;

internal interface IVerifyDispatcher
{
    Task VerifySucceededAsync(int applicationId);
    Task VerifyFailedAsync(int applicationId, string venueManagerId, string? failureMessage);
}
