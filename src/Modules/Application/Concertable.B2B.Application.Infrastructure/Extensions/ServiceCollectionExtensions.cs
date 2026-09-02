using Concertable.B2B.Infrastructure.Extensions;
using Concertable.B2B.Infrastructure.Services.Strategies;
using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Application.Mappers;
using Concertable.B2B.Application.Application.Renderers;
using Concertable.B2B.Application.Application.Strategies;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Application.Domain.Events;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.Application.Infrastructure.Data.Seeders;
using Concertable.B2B.Application.Infrastructure.Events;
using Concertable.B2B.Application.Infrastructure.Repositories;
using Concertable.B2B.Application.Infrastructure.Services;
using Concertable.B2B.Application.Infrastructure.Services.Payment;
using Concertable.B2B.Application.Infrastructure.Strategies;
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
                        provider.GetRequiredService<VenueArtistTenantInterceptor>(),
                        provider.GetRequiredService<IDomainEventDispatchInterceptor>())
                    .UseSeedingSupport(provider));

            services.AddDbContext<ApplicationReadDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddScoped<IApplicationReadDbContext>(provider =>
                provider.GetRequiredService<ApplicationReadDbContext>());

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
            services.AddScoped<IApplicationRepository, ApplicationRepository>();
            services.AddScoped<IApplicationEligibility, ApplicationEligibility>();
            services.AddScoped<ApplicationWorkflow>();
            services.AddScoped<IApplicationWorkflow>(provider =>
                provider.GetRequiredService<ApplicationWorkflow>());
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IApplicationDashboardService, ApplicationDashboardService>();
            services.AddScoped<IApplicationMapper, ApplicationMapper>();
            services.AddScoped<IApplicationNotifier, ApplicationNotifier>();
            services.AddScoped<IApplicationValidator, ApplicationValidator>();
            services.AddScoped<IConcertAvailability, ConcertAvailability>();
            services.AddScoped<IPaymentVerificationRecorder, PaymentVerificationRecorder>();
            services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, VerifyPaymentProcessor>();
            services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, VerifyPaymentFailedProcessor>();
            services.AddScoped<IDomainEventHandler<ApplicationCounterpartyNotifiedDomainEvent>,
                ApplicationCounterpartyNotifiedDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<ApplicationAcceptedDomainEvent>,
                ApplicationAcceptedDomainEventHandler>();
            services.AddScoped<ApplicationCancellationIntegrationEventHandler>();
            services.AddScoped<IIntegrationEventHandler<BookingCancelledEvent>>(provider =>
                provider.GetRequiredService<ApplicationCancellationIntegrationEventHandler>());
            services.AddScoped<IIntegrationEventHandler<ConcertCancelledEvent>>(provider =>
                provider.GetRequiredService<ApplicationCancellationIntegrationEventHandler>());
            services.AddScoped<ConcertAvailabilityIntegrationEventHandler>();
            services.AddScoped<IIntegrationEventHandler<ConcertCreatedEvent>>(provider =>
                provider.GetRequiredService<ConcertAvailabilityIntegrationEventHandler>());
            services.AddScoped<IIntegrationEventHandler<ConcertCancelledEvent>>(provider =>
                provider.GetRequiredService<ConcertAvailabilityIntegrationEventHandler>());
            services.AddScoped<IApplicationCheckoutService, ApplicationCheckoutService>();
            services.AddApplicationDealStrategies();
            services.AddApplicationDealUnions();
            services.AddScoped<IApplicationModule, ApplicationModule>();

            services.AddSingleton<ApplicationConfigurationProvider>();
            services.AddSingleton<IEntityTypeConfigurationProvider>(provider =>
                provider.GetRequiredService<ApplicationConfigurationProvider>());

            return services;
        }

        internal IServiceCollection AddApplicationDealStrategies()
        {
            services.AddScoped<IDealTermsRenderer, DealTermsRenderer>();
            return services.AddApplicationDealStrategies(builder =>
            {
                builder.For(DealType.FlatFee)
                    .AddSingleton<IDealTerms, FlatFeeDealTerms>();
                builder.For(DealType.DoorSplit)
                    .AddSingleton<IDealTerms, DoorSplitDealTerms>();
                builder.For(DealType.Versus)
                    .AddSingleton<IDealTerms, VersusDealTerms>();
                builder.For(DealType.VenueHire)
                    .AddSingleton<IDealTerms, VenueHireDealTerms>();
            });
        }

        internal IServiceCollection AddApplicationDealStrategies(
            Action<DealStrategyBuilder> configure)
        {
            var builder = new DealStrategyBuilder(services);
            configure(builder);
            builder.Build();

            return services;
        }

        internal IServiceCollection AddApplicationDealUnions()
        {
            services.AddApplicationDealUnion<Apply>(union =>
            {
                union.Case<IApplyStandard>(apply => new Apply.Standard(apply))
                    .Use<StandardApply>(
                        DealType.FlatFee,
                        DealType.DoorSplit,
                        DealType.Versus);
                union.Case<IApplyPrepaid>(apply => new Apply.Prepaid(apply))
                    .Use<PrepaidApply>(DealType.VenueHire);
            });

            services.AddApplicationDealUnion<Accept>(union =>
            {
                union.Case<IAccept>(accept => new Accept.Standard(accept))
                    .UseScoped<StandardAccept>(DealType.FlatFee)
                    .UseScoped<PrepaidAccept>(DealType.VenueHire);
                union.Case<IAcceptPaid>(accept => new Accept.Paid(accept))
                    .UseScoped<DoorSplitAccept>(DealType.DoorSplit)
                    .UseScoped<VersusAccept>(DealType.Versus);
            });

            return services;
        }

        internal IServiceCollection AddApplicationDealUnion<TUnion>(
            Action<DealUnionBuilder<TUnion>> configure)
        {
            var builder = new DealUnionBuilder<TUnion>(services);
            configure(builder);
            builder.Build();

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
