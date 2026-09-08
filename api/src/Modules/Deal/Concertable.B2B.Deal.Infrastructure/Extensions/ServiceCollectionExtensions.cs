using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Services;
using Concertable.B2B.Deal.Infrastructure.Data;
using Concertable.B2B.Deal.Infrastructure.Data.Seeders;
using Concertable.B2B.Deal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concertable.DataAccess.Infrastructure.Data;

namespace Concertable.B2B.Deal.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDealModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<DealDbContext>((sp, opt) =>
            opt.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IDealService, DealService>();
        services.AddScoped<IDealModule, DealModule>();

        services.AddSingleton<DealConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<DealConfigurationProvider>());

        return services;
    }

    public static IServiceCollection AddDealDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, DealDevSeeder>();
        return services;
    }

    public static IServiceCollection AddDealTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, DealTestSeeder>();
        return services;
    }
}
