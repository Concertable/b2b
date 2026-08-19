using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Admin.Application.Validators;
using Concertable.B2B.Admin.Domain.Events;
using Concertable.B2B.Admin.Infrastructure.Authorization;
using Concertable.B2B.Admin.Infrastructure.Data;
using Concertable.B2B.Admin.Infrastructure.Data.Seeders;
using Concertable.B2B.Admin.Infrastructure.Events;
using Concertable.B2B.Admin.Infrastructure.Settings;
using Concertable.Seed.Identity;
using Concertable.Seed.Shared;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Admin.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdminModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AdminDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>()));

        services.AddSingleton<AdminConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<AdminConfigurationProvider>());

        services.AddScoped<IAdminRepository, AdminRepository>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IAdminModule, AdminModule>();

        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .PostConfigure<IHostEnvironment>((options, env) =>
            {
                if (options.BootstrapEmail is null && !env.IsProduction())
                    options.BootstrapEmail = SeedUsers.AdminEmail;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", p => p.AddRequirements(new AdminProfileRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, AdminProfileHandler>();

        services.AddScoped<IIntegrationEventHandler<CredentialRegisteredEvent>, AdminProvisioningHandler>();
        services.AddScoped<IDomainEventHandler<AdminInvitationCreatedDomainEvent>, AdminInvitationCreatedDomainEventHandler>();

        services.AddValidatorsFromAssemblyContaining<CreateAdminInvitationRequestValidator>(includeInternalTypes: true);

        return services;
    }

    public static IServiceCollection AddAdminDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, AdminDevSeeder>();
        return services;
    }

    public static IServiceCollection AddAdminTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, AdminTestSeeder>();
        return services;
    }
}
