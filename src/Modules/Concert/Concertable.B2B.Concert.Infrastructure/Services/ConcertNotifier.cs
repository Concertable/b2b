namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class ConcertNotifier : IConcertNotifier
{
    private readonly INotificationClient notificationClient;

    public ConcertNotifier(INotificationClient notificationClient)
    {
        this.notificationClient = notificationClient;
    }

    public Task ConcertDraftCreatedAsync(string userId, object payload) =>
        notificationClient.SendAsync(userId, "ConcertDraftCreated", payload);

    public Task VerifyPaymentFailedAsync(string userId, object payload) =>
        notificationClient.SendAsync(userId, "VerifyPaymentFailed", payload);
}
