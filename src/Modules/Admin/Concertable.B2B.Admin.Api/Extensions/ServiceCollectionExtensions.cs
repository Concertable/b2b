using Concertable.B2B.Admin.Api.Controllers;
using Concertable.B2B.Admin.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Admin.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdminApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddAdminModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(AdminController).Assembly);
        return services;
    }
}
