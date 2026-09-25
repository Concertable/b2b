using Concertable.B2B.Application.Domain.Entities;

namespace Concertable.B2B.Application.Application.Interfaces;

internal interface IApplicationNotifier
{
    Task AppliedAsync(ApplicationEntity application);
    Task AcceptedAsync(ApplicationEntity application);
    Task WithdrawnAsync(ApplicationEntity application);
    Task RejectedAsync(ApplicationEntity application);
    Task CancelledAsync(ApplicationEntity application);

    /// <summary>
    /// Tells the venue manager who started the checkout that the card verification failed, so the acceptance
    /// they are waiting on has stopped. Delivered to the browser rather than the conversation, because it
    /// answers an action still in flight.
    /// </summary>
    Task VerifyPaymentFailedAsync(int applicationId, string failureMessage);
}
