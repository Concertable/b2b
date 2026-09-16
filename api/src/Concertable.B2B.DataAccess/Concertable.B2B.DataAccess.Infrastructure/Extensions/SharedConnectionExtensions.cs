using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

public static class SharedConnectionExtensions
{
    /// <summary>
    /// Registers the one connection every module's context is built on for the life of a request.
    /// <para>
    /// A protected write spans modules — accepting an application writes the application's transition, a
    /// booking, its contract, their access grants and an outbox row across three contexts — and has to
    /// commit or roll back as one. Contexts can only share a transaction when they share the connection it
    /// belongs to, so the ambient scope a module's <c>IUnitOfWorkBehavior</c> opens stays a local
    /// transaction instead of spanning several connections.
    /// </para>
    /// <para>
    /// One connection carries one command at a time. Nothing may run two contexts concurrently inside a
    /// request — no <c>Task.WhenAll</c> over two repositories.
    /// </para>
    /// </summary>
    public static IServiceCollection AddSharedDbConnection(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped(_ => new SqlConnection(configuration.GetConnectionString(B2BDb.Name)));
        services.AddScoped<DbConnection>(provider => provider.GetRequiredService<SqlConnection>());

        // Dapper resolves the same instance, so a raw read inside a write sees that write's own state.
        services.AddScoped<IDbConnection>(provider => provider.GetRequiredService<SqlConnection>());

        return services;
    }
}
