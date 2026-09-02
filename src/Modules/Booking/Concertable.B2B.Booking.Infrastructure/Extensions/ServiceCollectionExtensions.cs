using Concertable.B2B.Infrastructure.Extensions;
using Concertable.B2B.Infrastructure.Services.Strategies;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Application.Strategies;
using Concertable.B2B.Booking.Infrastructure.Events;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Data.Seeders;
using Concertable.B2B.Booking.Infrastructure.Repositories;
using Concertable.B2B.Booking.Infrastructure.Services;
using Concertable.B2B.Booking.Infrastructure.Strategies;
using Concertable.B2B.Booking.Domain.Events;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.Kernel;
using Concertable.Messaging.Contracts;
using Concertable.Payment.Contracts;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;

namespace Concertable.B2B.Booking.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddBookingModule(IConfiguration configuration)
        {
            services.AddDbContext<BookingDbContext>((provider, options) =>
                options.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                    .AddInterceptors(
                        provider.GetRequiredService<AuditInterceptor>(),
                        provider.GetRequiredService<TenantInterceptor>(),
                        provider.GetRequiredService<VenueArtistTenantInterceptor>(),
                        provider.GetRequiredService<IDomainEventDispatchInterceptor>())
                    .UseSeedingSupport(provider));

            services.AddDbContext<BookingReadDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddScoped<IBookingReadDbContext>(provider =>
                provider.GetRequiredService<BookingReadDbContext>());

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
            services.AddScoped<IOutboxUnitOfWorkBehavior, OutboxUnitOfWorkBehavior>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IBookingWorkflow, BookingWorkflow>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IContractPdfRenderer, ContractPdfRenderer>();
            services.AddScoped<IBookingModule, BookingModule>();
            services.AddBookingDealStrategies();
            services.AddScoped<IDomainEventHandler<ApplicationAcceptedDomainEvent>,
                ApplicationAcceptedDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<VerifyPaymentSucceededDomainEvent>,
                VerifyPaymentSucceededDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<VerifyPaymentFailedDomainEvent>,
                VerifyPaymentFailedDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<BookingCancelledDomainEvent>,
                BookingCancelledDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<BookingConfirmedDomainEvent>,
                BookingConfirmedDomainEventHandler>();
            services.AddScoped<AcceptanceFinancialOperationOutcomeProcessor>();
            services.AddScoped<IIntegrationEventHandler<CaptureEscrowSucceededEvent>>(provider =>
                provider.GetRequiredService<AcceptanceFinancialOperationOutcomeProcessor>());
            services.AddScoped<IIntegrationEventHandler<CaptureEscrowRejectedEvent>>(provider =>
                provider.GetRequiredService<AcceptanceFinancialOperationOutcomeProcessor>());
            services.AddScoped<IIntegrationEventHandler<DepositEscrowSucceededEvent>>(provider =>
                provider.GetRequiredService<AcceptanceFinancialOperationOutcomeProcessor>());
            services.AddScoped<IIntegrationEventHandler<DepositEscrowRejectedEvent>>(provider =>
                provider.GetRequiredService<AcceptanceFinancialOperationOutcomeProcessor>());
            services.AddScoped<CancellationFinancialOperationOutcomeProcessor>();
            services.AddScoped<IIntegrationEventHandler<RefundEscrowSucceededEvent>>(provider =>
                provider.GetRequiredService<CancellationFinancialOperationOutcomeProcessor>());
            services.AddScoped<IIntegrationEventHandler<RefundEscrowRejectedEvent>>(provider =>
                provider.GetRequiredService<CancellationFinancialOperationOutcomeProcessor>());

            services.AddSingleton<BookingConfigurationProvider>();
            services.AddSingleton<IEntityTypeConfigurationProvider>(provider =>
                provider.GetRequiredService<BookingConfigurationProvider>());

            return services;
        }

        internal IServiceCollection AddBookingDealStrategies() =>
            services.AddBookingDealStrategies(builder =>
            {
                builder.For(DealType.FlatFee)
                    .AddScoped<IConfirm, FlatFeeConfirm>()
                    .AddScoped<ICancel, EscrowCancel>();
                builder.For(DealType.DoorSplit)
                    .AddScoped<IConfirm, DeferredConfirm>()
                    .AddScoped<ICancel, ImmediateCancel>();
                builder.For(DealType.Versus)
                    .AddScoped<IConfirm, DeferredConfirm>()
                    .AddScoped<ICancel, ImmediateCancel>();
                builder.For(DealType.VenueHire)
                    .AddScoped<IConfirm, VenueHireConfirm>()
                    .AddScoped<ICancel, EscrowCancel>();
            });

        internal IServiceCollection AddBookingDealStrategies(
            Action<DealStrategyBuilder> configure)
        {
            var builder = new DealStrategyBuilder(services);
            configure(builder);
            builder.Build();
            return services;
        }

        public IServiceCollection AddBookingDevSeeder()
        {
            services.AddScoped<IDevSeeder, BookingDevSeeder>();
            return services;
        }

        public IServiceCollection AddBookingTestSeeder()
        {
            services.AddScoped<ITestSeeder, BookingTestSeeder>();
            return services;
        }
    }
}
