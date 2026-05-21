using Concertable.DataAccess;
using Concertable.DataAccess.Infrastructure;
using Concertable.Seeding;
using Concertable.Conversations.Application.Interfaces;
using Concertable.Conversations.Contracts;
using Concertable.Conversations.Infrastructure.Data;
using Concertable.Conversations.Infrastructure.Data.Seeders;
using Concertable.Conversations.Infrastructure.Repositories;
using Concertable.Conversations.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Conversations.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConversationsModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConversationsDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddSingleton<ConversationsConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ConversationsConfigurationProvider>());

        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConversationsNotifier, ConversationsNotifier>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IConversationsModule, ConversationsModule>();

        return services;
    }

    public static IServiceCollection AddConversationsDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, ConversationsDevSeeder>();
        return services;
    }

    public static IServiceCollection AddConversationsTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, ConversationsTestSeeder>();
        return services;
    }
}
