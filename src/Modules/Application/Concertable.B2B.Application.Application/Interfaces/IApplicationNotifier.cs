namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationNotifier
{
    Task AppliedAsync(int applicationId);
    Task AcceptedAsync(int applicationId);
    Task WithdrawnAsync(int applicationId);
    Task RejectedAsync(int applicationId);
    Task CancelledAsync(int applicationId);

    /// <summary>
    /// Tells the venue manager who started the checkout that the card verification failed, so the acceptance
    /// they are waiting on has stopped. Delivered to the browser rather than the conversation, because it
    /// answers an action still in flight.
    /// </summary>
    Task VerifyPaymentFailedAsync(int applicationId, string venueManagerId, string failureMessage);
}
