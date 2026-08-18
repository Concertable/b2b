using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Opportunity.Application.Mappers;
using Concertable.B2B.Opportunity.Application.Validators;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.Opportunity.Infrastructure.Repositories;
using Concertable.B2B.Opportunity.Infrastructure.Services;
using Concertable.B2B.Opportunity.Infrastructure.Sync;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Opportunity.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpportunityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OpportunityDbContext>((sp, options) =>
            options.UseSqlServer(
                    configuration.GetConnectionString(B2BDb.Name),
                    sql => sql.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantInterceptor>()));

        services.AddDbContext<OpportunityReadDbContext>(options =>
            options.UseSqlServer(
                    configuration.GetConnectionString(B2BDb.Name),
                    sql => sql.UseNetTopologySuite())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddScoped<IOpportunityReadDbContext>(
            sp => sp.GetRequiredService<OpportunityReadDbContext>());

        services.AddDbContext<OpportunityHandoffDbContext>((sp, options) =>
            options.UseSqlServer(
                    configuration.GetConnectionString(B2BDb.Name),
                    sql => sql.UseNetTopologySuite())
                .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        services.AddScoped<IUnitOfWork<OpportunityDbContext>, UnitOfWork<OpportunityDbContext>>();
        services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<IOpportunityReadRepository, OpportunityReadRepository>();
        services.AddScoped<IOpportunityHandoffRepository, OpportunityHandoffRepository>();
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<IOpportunityDashboardService, OpportunityDashboardService>();
        services.AddScoped<IOpportunityMapper, OpportunityMapper>();
        services.AddScoped<IOpportunitySyncer, OpportunitySyncer>();
        services.AddScoped<IOpportunityModule, OpportunityModule>();

        services.AddSingleton<OpportunityConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(
            sp => sp.GetRequiredService<OpportunityConfigurationProvider>());

        services.AddValidatorsFromAssemblyContaining<OpportunityDtoValidator>();

        return services;
    }
}
