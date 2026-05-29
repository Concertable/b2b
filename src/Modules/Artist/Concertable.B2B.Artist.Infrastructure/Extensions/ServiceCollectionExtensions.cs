using Concertable.B2B.Artist.Application.Validators;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Artist.Domain.Events;
using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.B2B.Artist.Infrastructure.Data.Seeders;
using Concertable.B2B.Artist.Infrastructure.Events;
using Concertable.B2B.Artist.Infrastructure.Handlers;
using Concertable.B2B.Artist.Infrastructure.Repositories;
using Concertable.B2B.Artist.Infrastructure.Services;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Seeding;
using Concertable.Seeding.Extensions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Artist.Infrastructure.Extensions;

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
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

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
