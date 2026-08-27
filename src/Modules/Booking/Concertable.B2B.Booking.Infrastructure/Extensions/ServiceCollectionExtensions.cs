using Concertable.B2B.Infrastructure.Extensions;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Booking.Application.Interfaces;
using Concertable.B2B.Booking.Infrastructure.Events;
using Concertable.B2B.Booking.Infrastructure.Data;
using Concertable.B2B.Booking.Infrastructure.Data.Seeders;
using Concertable.B2B.Booking.Infrastructure.Repositories;
using Concertable.B2B.Booking.Infrastructure.Services;
using Concertable.B2B.Booking.Infrastructure.Services.Strategies;
using Concertable.B2B.Booking.Domain.Events;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
                        provider.GetRequiredService<IDomainEventDispatchInterceptor>())
                    .UseSeedingSupport(provider));

            services.AddDbContext<BookingReadDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString(B2BDb.Name))
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddScoped<IBookingReadDbContext>(provider =>
                provider.GetRequiredService<BookingReadDbContext>());

            services.AddScoped<IUnitOfWork<BookingDbContext>, UnitOfWork<BookingDbContext>>();
            services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
            services.AddScoped<IOutboxUnitOfWorkBehavior, OutboxUnitOfWorkBehavior>();
            services.AddScoped<IBookingRepository, BookingRepository>();
            services.AddScoped<IContractRepository, ContractRepository>();
            services.AddScoped<IBookingService, BookingService>();
            services.AddScoped<IContractService, ContractService>();
            services.AddScoped<IContractPdfRenderer, ContractPdfRenderer>();
            services.AddScoped<IBookingModule, BookingModule>();
            services.AddBookingDealStrategies();
            services.AddScoped<IDomainEventHandler<ApplicationAcceptedDomainEvent>,
                ApplicationAcceptedDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<VerifyPaymentSucceeded>,
                VerifyPaymentSucceededHandler>();
            services.AddScoped<IDomainEventHandler<VerifyPaymentFailed>,
                VerifyPaymentFailedHandler>();
            services.AddScoped<IDomainEventHandler<BookingCancelledDomainEvent>,
                BookingCancelledDomainEventHandler>();
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
            services.AddBookingDealStrategies(strategies =>
            {
                strategies.For(DealType.FlatFee)
                    .AddScoped<IConfirmStep, FlatFeeConfirmStep>()
                    .AddScoped<ICancelStep, EscrowCancelStep>();
                strategies.For(DealType.DoorSplit)
                    .AddScoped<IConfirmStep, DoorSplitConfirmStep>()
                    .AddScoped<ICancelStep, ImmediateCancelStep>();
                strategies.For(DealType.Versus)
                    .AddScoped<IConfirmStep, VersusConfirmStep>()
                    .AddScoped<ICancelStep, ImmediateCancelStep>();
                strategies.For(DealType.VenueHire)
                    .AddScoped<IConfirmStep, VenueHireConfirmStep>()
                    .AddScoped<ICancelStep, EscrowCancelStep>();

                strategies.RequireAll<IConfirmStep>();
                strategies.RequireAll<ICancelStep>();
            });

        internal IServiceCollection AddBookingDealStrategies(
            Action<BookingDealStrategyBuilder> configure)
        {
            var builder = new BookingDealStrategyBuilder(services);
            configure(builder);
            builder.Build();

            services.AddDealTypeStrategies();
            services.TryAddScoped<IConfirmationExecutor, ConfirmationExecutor>();
            services.TryAddScoped<ICancellationExecutor, CancellationExecutor>();
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
