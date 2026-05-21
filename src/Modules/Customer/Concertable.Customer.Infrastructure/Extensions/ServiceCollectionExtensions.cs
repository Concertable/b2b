using Concertable.DataAccess;
using Concertable.Seeding;
using Concertable.Customer.Application.Interfaces;
using Concertable.Customer.Contracts;
using Concertable.Customer.Infrastructure.Data;
using Concertable.Customer.Infrastructure.Data.Seeders;
using Concertable.Customer.Infrastructure.Repositories;
using Concertable.Customer.Infrastructure.Services;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Customer.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CustomerDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString("B2BDb"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddSingleton<CustomerConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<CustomerConfigurationProvider>());

        services.AddScoped<IPreferenceRepository, PreferenceRepository>();
        services.AddScoped<IPreferenceService, PreferenceService>();
        services.AddScoped<ICustomerModule, CustomerModule>();

        return services;
    }

    public static IServiceCollection AddCustomerDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, CustomerDevSeeder>();
        return services;
    }

    public static IServiceCollection AddCustomerTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, CustomerTestSeeder>();
        return services;
    }
}
