using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Renderers;
using Concertable.B2B.Application.Application.Steps;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Application.Infrastructure.Data.Seeders;
using Concertable.B2B.Application.Infrastructure.Events;
using Concertable.B2B.Application.Infrastructure.Repositories;
using Concertable.B2B.Application.Infrastructure.Services;
using Concertable.B2B.Application.Infrastructure.Services.Payment;
using Concertable.B2B.Application.Infrastructure.Services.Strategies;
using Concertable.B2B.Application.Infrastructure.Validators;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts.Events;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;

namespace Concertable.B2B.Application.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApplicationModule(IConfiguration configuration)
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
            services.AddScoped<IApplicationDashboardMetricsProvider, ApplicationDashboardMetricsProvider>();
            services.AddScoped<IApplicationMapper, ApplicationMapper>();
            services.AddScoped<IApplicationNotifier, ApplicationNotifier>();
            services.AddScoped<IApplicationValidator, ApplicationValidator>();
            services.AddScoped<IPaymentVerificationRecorder, PaymentVerificationRecorder>();
            services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, VerifyPaymentProcessor>();
            services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, VerifyPaymentFailedProcessor>();
            services.AddScoped<IDomainEventHandler<ApplicationCounterpartyNotifiedDomainEvent>,
                ApplicationCounterpartyNotifiedDomainEventHandler>();
            services.AddScoped<ApplicationCancellationIntegrationEventHandler>();
            services.AddScoped<IIntegrationEventHandler<BookingCancelledEvent>>(provider =>
                provider.GetRequiredService<ApplicationCancellationIntegrationEventHandler>());
            services.AddScoped<IIntegrationEventHandler<ConcertCancelledEvent>>(provider =>
                provider.GetRequiredService<ApplicationCancellationIntegrationEventHandler>());
            services.AddScoped<IApplicationCheckoutService, ApplicationCheckoutService>();
            services.AddApplicationDealStrategies();
            services.AddScoped<ITermsFingerprintCalculator, TermsFingerprintCalculator>();
            services.AddScoped<IClientContext, ClientContextAccessor>();
            services.AddScoped<IApplicationModule, ApplicationModule>();

            services.AddSingleton<ApplicationConfigurationProvider>();
            services.AddSingleton<IEntityTypeConfigurationProvider>(provider =>
                provider.GetRequiredService<ApplicationConfigurationProvider>());

            return services;
        }

        internal IServiceCollection AddApplicationDealStrategies()
        {
            services.AddScoped<IDealTermsSerializer, DealTermsSerializer>();
            services.AddScoped<IDealTermsRenderer, DealTermsRenderer>();
            services.AddScoped<IAcceptFactory, AcceptFactory>();

            return services.AddApplicationDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddSingleton<IDealTerms, FlatFeeDealTerms>()
                    .AddSingleton<IAccept, FlatFeeAccept>();
                strategies.For(DealType.DoorSplit)
                    .AddSingleton<IDealTerms, DoorSplitDealTerms>()
                    .AddSingleton<IAccept, DoorSplitAccept>();
                strategies.For(DealType.Versus)
                    .AddSingleton<IDealTerms, VersusDealTerms>()
                    .AddSingleton<IAccept, VersusAccept>();
                strategies.For(DealType.VenueHire)
                    .AddSingleton<IDealTerms, VenueHireDealTerms>()
                    .AddSingleton<IAccept, VenueHireAccept>();

                strategies.RequireAll<IDealTerms>();
                strategies.RequireAll<IAccept>();
            });
        }

        internal IServiceCollection AddApplicationDealStrategies(
            Action<ApplicationDealStrategyBuilder> configure)
        {
            var builder = new ApplicationDealStrategyBuilder(services);
            configure(builder);
            builder.Build();

            services.TryAddScoped<IKeyedServiceProvider>(provider => (IKeyedServiceProvider)provider);
            services.TryAddScoped(
                typeof(IApplicationDealStrategyFactory<>),
                typeof(ApplicationDealStrategyFactory<>));
            return services;
        }

        public IServiceCollection AddApplicationDevSeeder()
        {
            services.AddScoped<IDevSeeder, ApplicationDevSeeder>();
            return services;
        }

        public IServiceCollection AddApplicationTestSeeder()
        {
            services.AddScoped<ITestSeeder, ApplicationTestSeeder>();
            return services;
        }
    }
}
