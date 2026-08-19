using Concertable.B2B.Concert.Api.Controllers;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Concert.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConcertApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddConcertModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(ConcertController).Assembly);
        return services;
    }

    public static IServiceCollection AddConcertDevSeeder(this IServiceCollection services) =>
        Concertable.B2B.Concert.Infrastructure.Extensions.ServiceCollectionExtensions
            .AddConcertDevSeeder(services);
}
