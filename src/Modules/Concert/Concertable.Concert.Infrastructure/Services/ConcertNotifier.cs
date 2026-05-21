namespace Concertable.Concert.Infrastructure.Services;

internal class ConcertNotifier : IConcertNotifier
{
    private readonly INotificationModule notification;

    public ConcertNotifier(INotificationModule notification)
    {
        this.notification = notification;
    }

    public Task ConcertDraftCreatedAsync(string userId, object payload) =>
        notification.SendAsync(userId, "ConcertDraftCreated", payload);

    public Task ConcertPostedAsync(string userId, object payload) =>
        notification.SendAsync(userId, "ConcertPosted", payload);

    public Task VerifyPaymentFailedAsync(string userId, object payload) =>
        notification.SendAsync(userId, "VerifyPaymentFailed", payload);
}
