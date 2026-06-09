using Concertable.B2B.Tenant.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Tenant.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenantApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTenantModule(configuration);
        return services;
    }
}
