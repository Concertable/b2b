using Concertable.Kernel.Serializers;
using Concertable.DataAccess.Infrastructure.Repositories;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Extensions;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddDatabase(configuration);

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSharedInfrastructure(configuration);
        services.AddScoped<AuditInterceptor>();
        services.AddScoped<TenantInterceptor>();
        services.AddScoped<IDomainEventDispatchInterceptor, DomainEventDispatchInterceptor>();

        services.AddReadDbContext(configuration);

        services.AddScoped<IDbConnection>(_ =>
            new SqlConnection(configuration.GetConnectionString("B2BDb")));

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddGeometry();

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IDapperRepository, DapperRepository>();

        return services;
    }

    public static IServiceCollection AddValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();

        return services;
    }

    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.Authority = configuration["Auth:Authority"] ?? configuration["services__auth__https__0"];
                options.Audience = "concertable.b2b.api";
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ClockSkew = TimeSpan.Zero,
                    ValidateIssuer = !environment.IsDevelopment(),
                    RoleClaimType = "role"
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hub"))
                            context.Token = accessToken;
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
