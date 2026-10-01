using System.Data;
using Concertable.B2B.Authorization.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

public static class CommandTransactionExtensions
{
    public static IServiceCollection AddCommandTransactions(
        this IServiceCollection services)
    {
        services.AddSingleton(provider =>
        {
            var builder = new NpgsqlDataSourceBuilder(GetConnectionString(provider));
            builder.UseNetTopologySuite();
            return builder.Build();
        });
        services.AddScoped<CommandTransactionAccessor>();
        services.AddScoped<ICommandAuthorizationContext, CommandAuthorizationContext>();
        services.AddSingleton<ICommandTransactionCommitter, CommandTransactionCommitter>();
        services.AddSingleton<ITransactionRunner>(provider => new TransactionRunner(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<NpgsqlDataSource>(),
            provider.GetRequiredService<ICommandTransactionCommitter>()));
        services.AddScoped(provider => new CommandTransactionFactory(
            provider.GetRequiredService<NpgsqlDataSource>(),
            provider.GetRequiredService<CommandTransactionAccessor>(),
            provider.GetRequiredService<Concertable.Messaging.Infrastructure.Outbox.IDbContextAccessor>(),
            provider.GetRequiredService<ICommandTransactionCommitter>()));
        services.AddTransient<IDbConnection>(provider =>
            provider.GetRequiredService<NpgsqlDataSource>().CreateConnection());

        return services;
    }

    private static string GetConnectionString(IServiceProvider provider) =>
        provider.GetRequiredService<IConfiguration>().GetConnectionString(B2BDb.Name)
        ?? throw new InvalidOperationException($"Connection string '{B2BDb.Name}' is required.");
}
