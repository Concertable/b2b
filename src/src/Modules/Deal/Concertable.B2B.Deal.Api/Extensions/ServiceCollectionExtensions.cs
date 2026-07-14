using Concertable.B2B.Deal.Api.Controllers;
using Concertable.B2B.Deal.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Deal.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDealApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDealModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(DealController).Assembly);
        return services;
    }
}
