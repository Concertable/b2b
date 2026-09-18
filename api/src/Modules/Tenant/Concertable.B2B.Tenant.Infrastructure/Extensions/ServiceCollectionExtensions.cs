using System.Data.Common;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Auth.Contracts.Events;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Tenant.Contracts.Events;
using Concertable.B2B.Tenant.Application;
using Concertable.B2B.Tenant.Application.Tax;
using Concertable.B2B.Tenant.Application.Interfaces;
using Concertable.B2B.Tenant.Application.Validators;
using FluentValidation;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.B2B.Tenant.Infrastructure.Authorization;
using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
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

        services.Configure<UkTaxComplianceOptions>(configuration.GetSection(UkTaxComplianceOptions.SectionName));

        services.AddSingleton<ITaxComplianceRules, UkTaxComplianceRules>();

        // VAT computation: region arithmetic + the registration policy over it; Concert consumes it only via ITenantModule.
        services.AddSingleton<IVatCalculator, UkVatCalculator>();
        services.AddSingleton<IVatPolicy, VatPolicy>();

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<MembershipRepository>();
        services.AddScoped<IMembershipRepository>(sp => sp.GetRequiredService<MembershipRepository>());
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddScoped<ITenantActivityRepository, TenantActivityRepository>();
        services.AddScoped<ITenantActivityService, TenantActivityService>();
        services.AddScoped<IVerificationRepository, VerificationRepository>();
        services.AddScoped<IVerificationService, VerificationService>();
        services.AddScoped<IVerificationNotifier, VerificationNotifier>();
        services.AddScoped<ITenantModule, TenantModule>();

        // Tenant owns membership rows; the Authorization module owns request authority and reads them through this port.
        services.AddScoped<IMembershipReadRepository>(sp => sp.GetRequiredService<MembershipRepository>());
        services.AddScoped<IMembershipAuthorityFence>(sp => sp.GetRequiredService<MembershipRepository>());

        services.Configure<MvcOptions>(options => options.Filters.Add<BusinessProfileAuthorizationFilter>());

        services.AddScoped<IIntegrationEventHandler<CredentialRegisteredEvent>, TenantProvisioningHandler>();
        services.AddScoped<IIntegrationEventHandler<TenantActivityRecordedEvent>, TenantActivityRecordedHandler>();
        services.AddScoped<IDomainEventHandler<TenantCreatedDomainEvent>, TenantCreatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<TenantInvitationCreatedDomainEvent>, TenantInvitationCreatedDomainEventHandler>();

        // includeInternalTypes: the Tenant validators are internal — without it they're never registered and the VAT-format rule silently doesn't run (mirrors Concert).
        services.AddValidatorsFromAssemblyContaining<UpdateTenantRequestValidator>(includeInternalTypes: true);

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
