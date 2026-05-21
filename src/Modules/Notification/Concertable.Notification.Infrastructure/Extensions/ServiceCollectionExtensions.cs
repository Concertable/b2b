using Concertable.Notification.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Notification.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationModule(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<INotificationModule, SignalRNotificationModule>();
        return services;
    }
}
