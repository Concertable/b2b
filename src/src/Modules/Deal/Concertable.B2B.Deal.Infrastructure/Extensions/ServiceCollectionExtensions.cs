using Concertable.B2B.KeyedStrategies;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Deal.Application.Interfaces;
using Concertable.B2B.Deal.Application.Mappers;
using Concertable.B2B.Deal.Application.Services;
using Concertable.B2B.Deal.Application.Strategies;
using Concertable.B2B.Deal.Contracts.Enums;
using Concertable.B2B.Deal.Infrastructure.Data;
using Concertable.B2B.Deal.Infrastructure.Data.Seeders;
using Concertable.B2B.Deal.Infrastructure.Repositories;
using Concertable.B2B.Deal.Infrastructure.Services.Strategies;
using Concertable.B2B.Deal.Infrastructure.Services.Updaters;
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

        services.AddDealStrategies();

        services.AddSingleton<DealConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<DealConfigurationProvider>());

        return services;
    }

    internal static IServiceCollection AddDealStrategies(this IServiceCollection services)
    {
        services.AddScoped<IDealMapper, DealMapper>();
        services.AddScoped<IDealUpdater, DealUpdater>();

        return services.AddDealStrategies(strategies =>
        {
            strategies.For(DealType.FlatFee)
                .AddSingleton<IDealMapper, FlatFeeDealMapper>()
                .AddSingleton<IDealUpdater, FlatFeeDealUpdater>();
            strategies.For(DealType.DoorSplit)
                .AddSingleton<IDealMapper, DoorSplitDealMapper>()
                .AddSingleton<IDealUpdater, DoorSplitDealUpdater>();
            strategies.For(DealType.Versus)
                .AddSingleton<IDealMapper, VersusDealMapper>()
                .AddSingleton<IDealUpdater, VersusDealUpdater>();
            strategies.For(DealType.VenueHire)
                .AddSingleton<IDealMapper, VenueHireDealMapper>()
                .AddSingleton<IDealUpdater, VenueHireDealUpdater>();

            strategies.RequireAll<IDealMapper>();
            strategies.RequireAll<IDealUpdater>();
        });
    }

    internal static IServiceCollection AddDealStrategies(
        this IServiceCollection services,
        Action<KeyedStrategyBuilder<DealType>> configure)
    {
        var builder = new KeyedStrategyBuilder<DealType>(services);
        configure(builder);
        builder.Build();

        services.TryAddScoped<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);
        services.TryAddScoped(typeof(IDealStrategyFactory<>), typeof(DealStrategyFactory<>));
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
