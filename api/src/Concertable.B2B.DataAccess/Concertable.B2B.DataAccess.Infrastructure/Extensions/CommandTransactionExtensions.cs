using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

public static class CommandTransactionExtensions
{
    public static IServiceCollection AddCommandTransactions(
        this IServiceCollection services)
    {
        services.AddScoped<CommandTransactionAccessor>();
        services.AddSingleton<ICommandExecutor>(provider => new CommandExecutor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            GetConnectionString(provider)));
        services.AddScoped(provider => new CommandTransactionFactory(
            GetConnectionString(provider),
            provider.GetRequiredService<CommandTransactionAccessor>(),
            provider.GetRequiredService<Concertable.Messaging.Infrastructure.Outbox.IDbContextAccessor>()));
        services.AddTransient<IDbConnection>(provider => new SqlConnection(GetConnectionString(provider)));

        return services;
    }

    private static string GetConnectionString(IServiceProvider provider) =>
        provider.GetRequiredService<IConfiguration>().GetConnectionString(B2BDb.Name)
        ?? throw new InvalidOperationException($"Connection string '{B2BDb.Name}' is required.");
}
