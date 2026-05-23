namespace Concertable.B2B.Notification.Contracts;

public interface INotificationClient
{
    Task SendAsync(string userId, string eventName, object payload);
}
