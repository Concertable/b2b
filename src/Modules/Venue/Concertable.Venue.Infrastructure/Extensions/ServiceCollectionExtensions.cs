using Concertable.Concert.Contracts.Events;
using Concertable.DataAccess.Infrastructure;
using Concertable.Seeding;
using Concertable.Venue.Application.Validators;
using Concertable.Venue.Domain.Events;
using Concertable.Venue.Infrastructure.Data;
using Concertable.Venue.Infrastructure.Data.Seeders;
using Concertable.Venue.Infrastructure.Events;
using Concertable.Venue.Infrastructure.Handlers;
using Concertable.Venue.Infrastructure.Repositories;
using Concertable.Venue.Infrastructure.Services;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Venue.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVenueModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<VenueDbContext>((sp, opt) =>
            opt.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOpt => sqlOpt.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IVenueDashboardService, VenueDashboardService>();
        services.AddScoped<IVenueReviewService, VenueReviewService>();
        services.AddScoped<IVenueRepository, VenueRepository>();
        services.AddScoped<IVenueModule, VenueModule>();
        services.AddScoped<IIntegrationEventHandler<ReviewSubmittedEvent>, VenueReviewProjectionHandler>();
        services.AddScoped<IDomainEventHandler<VenueChangedDomainEvent>, VenueChangedDomainEventHandler>();

        services.AddSingleton<VenueConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<VenueConfigurationProvider>());
        services.AddSingleton<IRatingProjectionConfigurationProvider, VenueRatingProjectionConfigurationProvider>();

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
