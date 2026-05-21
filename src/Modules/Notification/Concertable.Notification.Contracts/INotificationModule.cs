namespace Concertable.Notification.Contracts;

public interface INotificationModule
{
    Task SendAsync(string userId, string eventName, object payload);
}
