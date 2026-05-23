using Concertable.B2B.Organization.Contracts;
using Concertable.B2B.Organization.Application.Interfaces;
using Concertable.B2B.Organization.Infrastructure.Data;
using Concertable.B2B.Organization.Infrastructure.Repositories;
using Concertable.B2B.Organization.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;

namespace Concertable.B2B.Organization.Infrastructure.Extensions;

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
