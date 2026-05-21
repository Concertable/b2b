using Concertable.DataAccess;
using Concertable.DataAccess.Infrastructure;
using Concertable.Seeding;
using Concertable.Shared.Email;
using Concertable.Artist.Contracts.Events;
using Concertable.User.Application.Validators;
using Concertable.User.Domain.Events;
using Concertable.User.Infrastructure.Authorization;
using Concertable.User.Infrastructure.Data;
using Concertable.User.Infrastructure.Data.Seeders;
using Concertable.User.Infrastructure.Events;
using Concertable.User.Infrastructure.Services.Email;
using Concertable.Venue.Contracts.Events;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>((sp, opt) =>
            opt.UseSqlServer(
                    configuration.GetConnectionString("B2BDb"),
                    sqlOpt => sqlOpt.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserModule, UserModule>();

        var useRealEmail = configuration.GetSection("ExternalServices").GetValue<bool>("UseRealEmail");
        if (!useRealEmail)
            services.AddScoped<IEmailSender, AutoVerifyingFakeEmailSender>();

        services.AddScoped<IDomainEventHandler<UserCreatedDomainEvent>, UserCreatedDomainEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ArtistChangedEvent>, ArtistManagerSyncHandler>();
        services.AddScoped<IIntegrationEventHandler<VenueChangedEvent>, VenueManagerSyncHandler>();

        services.AddSingleton<UserConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<UserConfigurationProvider>());

        services.AddValidatorsFromAssemblyContaining<UpdateLocationRequestValidator>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("VenueManager", p => p.AddRequirements(new VenueManagerProfileRequirement()));
            options.AddPolicy("ArtistManager", p => p.AddRequirements(new ArtistManagerProfileRequirement()));
            options.AddPolicy("Admin", p => p.AddRequirements(new AdminProfileRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, VenueManagerProfileHandler>();
        services.AddScoped<IAuthorizationHandler, ArtistManagerProfileHandler>();
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
