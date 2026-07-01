using Concertable.B2B.Tenant.Api.Controllers;
using Concertable.B2B.Tenant.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Tenant.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenantApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTenantModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(TenantController).Assembly);
        return services;
    }
}
