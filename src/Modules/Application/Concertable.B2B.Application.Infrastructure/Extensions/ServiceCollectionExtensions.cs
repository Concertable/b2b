using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Renderers;
using Concertable.B2B.Application.Application.Steps;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Application.Infrastructure.Data.Seeders;
using Concertable.B2B.Application.Infrastructure.Events;
using Concertable.B2B.Application.Infrastructure.Repositories;
using Concertable.B2B.Application.Infrastructure.Services;
using Concertable.B2B.Application.Infrastructure.Services.Payment;
using Concertable.B2B.Application.Infrastructure.Validators;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;

namespace Concertable.B2B.Application.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LegalSettings>(configuration.GetSection(LegalSettings.SectionName));
        services.AddDbContext<ApplicationDbContext>((provider, options) =>
            options.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .AddInterceptors(
                    provider.GetRequiredService<AuditInterceptor>(),
                    provider.GetRequiredService<TenantInterceptor>(),
                    provider.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(provider));

        services.AddDbContext<ApplicationReadDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
        services.AddScoped<IApplicationReadDbContext>(provider =>
            provider.GetRequiredService<ApplicationReadDbContext>());

        services.AddScoped<IUnitOfWork<ApplicationDbContext>, UnitOfWork<ApplicationDbContext>>();
        services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IApplicationDashboardService, ApplicationDashboardService>();
        services.AddScoped<IApplicationMapper, ApplicationMapper>();
        services.AddScoped<IApplicationNotifier, ApplicationNotifier>();
        services.AddScoped<IApplicationValidator, ApplicationValidator>();
        services.AddScoped<IPaymentVerificationRecorder, PaymentVerificationRecorder>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, VerifyPaymentProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, VerifyPaymentFailedProcessor>();
        services.AddScoped<IDomainEventHandler<ApplicationCounterpartyNotifiedDomainEvent>,
            ApplicationCounterpartyNotifiedDomainEventHandler>();
        services.AddScoped<IApplicationCheckoutService, ApplicationCheckoutService>();
        services.AddScoped(typeof(IStepResolver<>), typeof(StepResolver<>));
        services.AddScoped<IDealTermsSerializer, DealTermsSerializer>();
        services.AddScoped<IDealTermsRenderer, DealTermsRenderer>();
        services.AddKeyedScoped<IDealTerms, FlatFeeDealTerms>(DealType.FlatFee);
        services.AddKeyedScoped<IDealTerms, DoorSplitDealTerms>(DealType.DoorSplit);
        services.AddKeyedScoped<IDealTerms, VersusDealTerms>(DealType.Versus);
        services.AddKeyedScoped<IDealTerms, VenueHireDealTerms>(DealType.VenueHire);
        services.AddKeyedScoped<IAcceptStep, FlatFeeAcceptStep>(DealType.FlatFee);
        services.AddKeyedScoped<IAcceptStep, DoorSplitAcceptStep>(DealType.DoorSplit);
        services.AddKeyedScoped<IAcceptStep, VersusAcceptStep>(DealType.Versus);
        services.AddKeyedScoped<IAcceptStep, VenueHireAcceptStep>(DealType.VenueHire);
        services.AddScoped<ITermsFingerprintCalculator, TermsFingerprintCalculator>();
        services.AddScoped<IClientContext, ClientContextAccessor>();
        services.AddScoped<IApplicationModule, ApplicationModule>();

        services.AddSingleton<ApplicationConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(provider =>
            provider.GetRequiredService<ApplicationConfigurationProvider>());

        return services;
    }

    public static IServiceCollection AddApplicationDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, ApplicationDevSeeder>();
        return services;
    }

    public static IServiceCollection AddApplicationTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, ApplicationTestSeeder>();
        return services;
    }
}
