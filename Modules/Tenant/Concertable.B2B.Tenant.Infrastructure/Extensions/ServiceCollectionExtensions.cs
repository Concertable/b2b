using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Validators;
using FluentValidation;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Data.Seeders;
using Concertable.B2B.Tenant.Infrastructure.Events;
using Concertable.B2B.Tenant.Infrastructure.Repositories;
using Concertable.B2B.Tenant.Infrastructure.Services;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Tenant.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTenantModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TenantDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>()));

        services.AddSingleton<TenantConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<TenantConfigurationProvider>());

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<ITenantModule, TenantModule>();

        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantResolver>(sp => sp.GetRequiredService<TenantContext>());

        services.AddScoped<IIntegrationEventHandler<CredentialRegisteredEvent>, TenantProvisioningHandler>();
        services.AddScoped<IDomainEventHandler<TenantCreatedDomainEvent>, TenantCreatedDomainEventHandler>();

        services.AddValidatorsFromAssemblyContaining<UpdateTenantRequestValidator>();

        return services;
    }

    public static IServiceCollection AddTenantDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, TenantDevSeeder>();
        return services;
    }

    public static IServiceCollection AddTenantTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, TenantTestSeeder>();
        return services;
    }
}
