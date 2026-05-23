using Concertable.Artist.Application.Validators;
using Concertable.Artist.Contracts;
using Concertable.Artist.Domain.Events;
using Concertable.Artist.Infrastructure.Data;
using Concertable.Artist.Infrastructure.Data.Seeders;
using Concertable.DataAccess.Infrastructure;
using Concertable.Artist.Infrastructure.Events;
using Concertable.Artist.Infrastructure.Handlers;
using Concertable.Artist.Infrastructure.Repositories;
using Concertable.Artist.Infrastructure.Services;
using Concertable.Concert.Contracts.Events;
using Concertable.Seeding;
using Concertable.Shared;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Artist.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddArtistModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ArtistDbContext>((sp, opt) =>
            opt.UseSqlServer(
                    configuration.GetConnectionString("B2BDb"),
                    sqlOpt => sqlOpt.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<IArtistDashboardService, ArtistDashboardService>();
        services.AddScoped<IArtistReviewService, ArtistReviewService>();
        services.AddScoped<IArtistRepository, ArtistRepository>();
        services.AddScoped<IArtistModule, ArtistModule>();
        services.AddScoped<IIntegrationEventHandler<CustomerReviewSubmittedEvent>, ArtistReviewProjectionHandler>();
        services.AddScoped<IDomainEventHandler<ArtistChangedDomainEvent>, ArtistChangedDomainEventHandler>();

        services.AddSingleton<ArtistConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ArtistConfigurationProvider>());
        services.AddSingleton<IRatingProjectionConfigurationProvider, ArtistRatingProjectionConfigurationProvider>();

        services.AddValidatorsFromAssemblyContaining<CreateArtistRequestValidator>();

        return services;
    }

    public static IServiceCollection AddArtistDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, ArtistDevSeeder>();
        return services;
    }

    public static IServiceCollection AddArtistTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, ArtistTestSeeder>();
        return services;
    }
}
