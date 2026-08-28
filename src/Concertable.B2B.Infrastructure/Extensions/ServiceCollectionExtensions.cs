using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Infrastructure.Context;
using Concertable.B2B.RequestContext;
using Concertable.B2B.Infrastructure.Services.Strategies;
using Concertable.B2B.Infrastructure.Uris;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Concertable.B2B.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClientContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IClientContext, ClientContextAccessor>();

        return services;
    }

    public static IServiceCollection AddDealTypeStrategies(this IServiceCollection services)
    {
        services.TryAddScoped<IKeyedServiceProvider>(provider => (IKeyedServiceProvider)provider);
        services.TryAddScoped(typeof(IDealTypeStrategyFactory<>), typeof(DealTypeStrategyFactory<>));

        return services;
    }

    public static IServiceCollection AddUris(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FrontendUrlSettings>(configuration.GetSection(FrontendUrlSettings.SectionName));
        services.AddSingleton<IUriGenerator, UriGenerator>();
        services.AddSingleton<IFrontendUriGenerator, FrontendUriGenerator>();

        return services;
    }
}
