using Concertable.Notification.Contracts;
using Concertable.Notification.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Concertable.Notification.Infrastructure;

internal class SignalRNotificationClient : INotificationClient
{
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly ILogger<SignalRNotificationClient> logger;

    public SignalRNotificationClient(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationClient> logger)
    {
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public Task SendAsync(string userId, string eventName, object payload)
    {
        logger.LogInformation("[SignalRNotificationClient] send userId={UserId} event={EventName}", userId, eventName);
        return hubContext.Clients.Group(userId).SendAsync(eventName, payload);
    }
}
