using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Opportunity.Application.Mappers;
using Concertable.B2B.Opportunity.Application.Validators;
using Concertable.B2B.Opportunity.Infrastructure.Data;
using Concertable.B2B.Opportunity.Infrastructure.Data.Seeders;
using Concertable.B2B.Opportunity.Infrastructure.Repositories;
using Concertable.B2B.Opportunity.Infrastructure.Services;
using Concertable.B2B.Opportunity.Infrastructure.Sync;
using Concertable.B2B.Opportunity.Infrastructure.Events;
using Concertable.B2B.Application.Contracts.Events;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Messaging.Contracts;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;

namespace Concertable.B2B.Opportunity.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpportunityModule(IConfiguration configuration)
        {
            services.AddDbContext<OpportunityDbContext>((sp, options) =>
                options.UseSqlServer(
                        configuration.GetConnectionString(B2BDb.Name),
                        sql => sql.UseNetTopologySuite())
                    .AddInterceptors(
                        sp.GetRequiredService<AuditInterceptor>(),
                        sp.GetRequiredService<TenantInterceptor>())
                    .UseSeedingSupport(sp));

            services.AddDbContext<OpportunityReadDbContext>(options =>
                options.UseSqlServer(
                        configuration.GetConnectionString(B2BDb.Name),
                        sql => sql.UseNetTopologySuite())
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddScoped<IOpportunityReadDbContext>(
                sp => sp.GetRequiredService<OpportunityReadDbContext>());

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
            services.AddScoped<IOpportunityRepository, OpportunityRepository>();
            services.AddScoped<IOpportunityReadRepository, OpportunityReadRepository>();
            services.AddScoped<IOpportunityService, OpportunityService>();
            services.AddScoped<IOpportunitySyncer, OpportunitySyncer>();
            services.AddScoped<IOpportunityModule, OpportunityModule>();
            services.AddScoped<OpportunityCancellationIntegrationEventHandler>();
            services.AddScoped<IIntegrationEventHandler<BookingCancelledEvent>>(provider =>
                provider.GetRequiredService<OpportunityCancellationIntegrationEventHandler>());
            services.AddScoped<IIntegrationEventHandler<ConcertCancelledEvent>>(provider =>
                provider.GetRequiredService<OpportunityCancellationIntegrationEventHandler>());
            services.AddScoped<IIntegrationEventHandler<ApplicationAcceptedEvent>,
                ApplicationAcceptedIntegrationEventHandler>();

            services.AddSingleton<OpportunityConfigurationProvider>();
            services.AddSingleton<IEntityTypeConfigurationProvider>(
                sp => sp.GetRequiredService<OpportunityConfigurationProvider>());

            services.AddValidatorsFromAssemblyContaining<OpportunityRequestValidator>(
                includeInternalTypes: true);

            return services;
        }

        public IServiceCollection AddOpportunityDevSeeder()
        {
            services.AddScoped<IDevSeeder, OpportunityDevSeeder>();
            return services;
        }

        public IServiceCollection AddOpportunityTestSeeder()
        {
            services.AddScoped<ITestSeeder, OpportunityTestSeeder>();
            return services;
        }
    }
}
