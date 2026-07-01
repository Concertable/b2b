using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Auth.Contracts.Events;
using Concertable.DataAccess;
using Concertable.B2B.User.Infrastructure.Mappers;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.B2B.User.Application.Validators;
using Concertable.B2B.User.Infrastructure.Authorization;
using Concertable.B2B.User.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Data.Seeders;
using Concertable.B2B.User.Infrastructure.Events;
using Concertable.B2B.Venue.Contracts.Events;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>((sp, opt) =>
            opt.UseSqlServer(
                    configuration.GetConnectionString(B2BDb.Name),
                    sqlOpt => sqlOpt.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

        services.AddKeyedScoped<IRoleMapper, VenueManagerMapper>(Role.VenueManager);
        services.AddKeyedScoped<IRoleMapper, ArtistManagerMapper>(Role.ArtistManager);
        services.AddKeyedScoped<IRoleMapper, AdminMapper>(Role.Admin);
        services.AddScoped<IUserMapper, UserMapper>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserModule, UserModule>();

        services.AddScoped<IIntegrationEventHandler<CredentialRegisteredEvent>, CredentialRegisteredHandler>();
        services.AddScoped<IIntegrationEventHandler<ArtistChangedEvent>, ArtistManagerSyncHandler>();
        services.AddScoped<IIntegrationEventHandler<VenueChangedEvent>, VenueManagerSyncHandler>();

        services.AddHealthChecks().AddCheck<UserHealthCheck>("users");

        services.AddSingleton<UserConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<UserConfigurationProvider>());

        services.AddValidatorsFromAssemblyContaining<UpdateLocationRequestValidator>();

        /* Admin alone survives the permission sweep — it gates platform endpoints orthogonal to tenancy
           (it checks the AdminProfile row by sub, not a tenant membership). Venue/artist authorization is now
           permission + persona via the Tenant module's PermissionPolicyProvider. */
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", p => p.AddRequirements(new AdminProfileRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, AdminProfileHandler>();

        return services;
    }

    public static IServiceCollection AddUserDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, UserDevSeeder>();
        return services;
    }

    public static IServiceCollection AddUserTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, UserTestSeeder>();
        return services;
    }
}
