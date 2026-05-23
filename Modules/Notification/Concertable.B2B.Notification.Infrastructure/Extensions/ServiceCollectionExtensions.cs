using Concertable.B2B.Notification.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Notification.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationClient(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<INotificationClient, SignalRNotificationClient>();
        return services;
    }
}
