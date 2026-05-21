using Concertable.DataAccess;
using Concertable.Seeding;
using Concertable.Contract.Application.Interfaces;
using Concertable.Contract.Application.Mappers;
using Concertable.Contract.Application.Services;
using Concertable.Contract.Infrastructure.Data;
using Concertable.Contract.Infrastructure.Data.Seeders;
using Concertable.Contract.Infrastructure.Repositories;
using Concertable.Contract.Infrastructure.Services.Updaters;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Contract.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContractModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContractDbContext>((sp, opt) =>
            opt.UseSqlServer(configuration.GetConnectionString("B2BDb"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IContractMapper, ContractMapper>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IContractModule, ContractModule>();

        services.AddSingleton<IContractUpdater, ContractUpdater>();
        services.AddSingleton<FlatFeeContractUpdater>();
        services.AddSingleton<DoorSplitContractUpdater>();
        services.AddSingleton<VersusContractUpdater>();
        services.AddSingleton<VenueHireContractUpdater>();

        services.AddSingleton<ContractConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ContractConfigurationProvider>());

        return services;
    }

    public static IServiceCollection AddContractDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, ContractDevSeeder>();
        return services;
    }

    public static IServiceCollection AddContractTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, ContractTestSeeder>();
        return services;
    }
}
