using Concertable.B2B.Organization.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Organization.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOrganizationModule(configuration);
        return services;
    }
}
