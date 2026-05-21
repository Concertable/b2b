using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Concertable.Authorization.Contracts;

namespace Concertable.Notification.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        this.logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        string? userId = Context.User?.GetId();

        logger.LogInformation(
            "[NotificationHub] connected userId={UserId} userIdentifier={UserIdentifier} connectionId={ConnectionId}",
            userId, Context.UserIdentifier, Context.ConnectionId);

        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        string? userId = Context.User?.GetId();

        if (!string.IsNullOrEmpty(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);

        await base.OnDisconnectedAsync(exception);
    }
}
