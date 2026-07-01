using Concertable.B2B.User.Api.Controllers;
using Concertable.B2B.User.Infrastructure.Extensions;
using Concertable.Shared.Api.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.User.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddUserModule(configuration);
        services.AddAuthorization(options =>
        {
            options.AddPolicy("UserClaimsScope", p =>
                p.RequireClaim("scope", "user:claims"));
        });
        services.AddControllers()
            .AddInternalControllers(typeof(UserController).Assembly);
        return services;
    }
}
