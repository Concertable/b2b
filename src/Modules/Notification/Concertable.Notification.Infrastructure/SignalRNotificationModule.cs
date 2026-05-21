using Concertable.Notification.Contracts;
using Concertable.Notification.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Concertable.Notification.Infrastructure;

internal class SignalRNotificationModule : INotificationModule
{
    private readonly IHubContext<NotificationHub> hubContext;
    private readonly ILogger<SignalRNotificationModule> logger;

    public SignalRNotificationModule(IHubContext<NotificationHub> hubContext, ILogger<SignalRNotificationModule> logger)
    {
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public Task SendAsync(string userId, string eventName, object payload)
    {
        logger.LogInformation("[SignalRNotificationModule] send userId={UserId} event={EventName}", userId, eventName);
        return hubContext.Clients.Group(userId).SendAsync(eventName, payload);
    }
}
