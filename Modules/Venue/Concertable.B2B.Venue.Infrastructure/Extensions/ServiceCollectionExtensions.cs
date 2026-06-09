using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Venue.Application.Validators;
using Concertable.B2B.Venue.Domain.Events;
using Concertable.B2B.Venue.Infrastructure.Data;
using Concertable.B2B.Venue.Infrastructure.Data.Seeders;
using Concertable.B2B.Venue.Infrastructure.Events;
using Concertable.B2B.Venue.Infrastructure.Handlers;
using Concertable.B2B.Venue.Infrastructure.Repositories;
using Concertable.B2B.Venue.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Venue.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVenueModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VenueDbContext>((sp, opt) =>
            opt.UseSqlServer(
                    configuration.GetConnectionString("B2BDb"),
                    sqlOpt => sqlOpt.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IVenueDashboardService, VenueDashboardService>();
        services.AddScoped<IVenueReviewService, VenueReviewService>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IVenueModule, VenueModule>();
        services.AddScoped<IIntegrationEventHandler<CustomerReviewSubmittedEvent>, VenueReviewProjectionHandler>();
        services.AddScoped<IDomainEventHandler<VenueChangedDomainEvent>, VenueChangedDomainEventHandler>();

        services.AddSingleton<VenueConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<VenueConfigurationProvider>());

        services.AddValidatorsFromAssemblyContaining<CreateVenueRequestValidator>();

        return services;
    }

    public static IServiceCollection AddVenueDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, VenueDevSeeder>();
        return services;
    }

    public static IServiceCollection AddVenueTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, VenueTestSeeder>();
        return services;
    }
}
