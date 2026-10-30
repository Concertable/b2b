using Concertable.B2B.Privacy.Api.Controllers;
using Concertable.B2B.Privacy.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Privacy.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPrivacyApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPrivacyModule(configuration);
        services.AddControllers()
            .AddInternalControllers(typeof(SubjectRightsController).Assembly);
        return services;
    }
}
