using Concertable.DataAccess;
using Concertable.Seed;
using Concertable.Seed.Extensions;
using Concertable.B2B.Contract.Application.Interfaces;
using Concertable.B2B.Contract.Application.Mappers;
using Concertable.B2B.Contract.Application.Services;
using Concertable.B2B.Contract.Infrastructure.Data;
using Concertable.B2B.Contract.Infrastructure.Data.Seeders;
using Concertable.B2B.Contract.Infrastructure.Repositories;
using Concertable.B2B.Contract.Infrastructure.Services.Updaters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;

namespace Concertable.B2B.Contract.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddContractModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ContractDbContext>((sp, opt) =>
            opt.UseSqlServer(configuration.GetConnectionString("B2BDb"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

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
