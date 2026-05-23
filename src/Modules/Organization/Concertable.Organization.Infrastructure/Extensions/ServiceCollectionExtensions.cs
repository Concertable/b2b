using Concertable.Organization.Contracts;
using Concertable.Organization.Application.Interfaces;
using Concertable.Organization.Infrastructure.Data;
using Concertable.Organization.Infrastructure.Repositories;
using Concertable.Organization.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;

namespace Concertable.Organization.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<OrganizationDbContext>((sp, opts) =>
            opts.UseSqlServer(configuration.GetConnectionString("B2BDb"))
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddSingleton<OrganizationConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<OrganizationConfigurationProvider>());

        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IOrganizationModule, OrganizationModule>();

        return services;
    }
}
