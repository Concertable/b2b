using System.Data.Common;
using Concertable.B2B.Infrastructure.Extensions;
using Concertable.B2B.Infrastructure.Services.Strategies;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Application.Validators;
using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Booking.Contracts.Events;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Contracts.Commands;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Data.Seeders;
using Concertable.B2B.Concert.Infrastructure.Emails;
using Concertable.B2B.Concert.Infrastructure.Events;
using Concertable.B2B.Concert.Infrastructure.Handlers;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.B2B.Concert.Infrastructure.Repositories;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Concert.Infrastructure.Strategies;
using Concertable.B2B.Concert.Infrastructure.Services.Settlement;
using Concertable.B2B.Concert.Infrastructure.Services.Completion;
using Concertable.B2B.Concert.Infrastructure.Services.Payment;
using Concertable.Customer.Ticket.Contracts.Events;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.B2B.Concert.Infrastructure.Validators;
using Concertable.B2B.Venue.Contracts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddConcertModule(IConfiguration configuration)
        {
            services.AddDbContextFactory<ConcertDbContext>((sp, opts) =>
                opts.UseSqlServer(
                        configuration.GetConnectionString(B2BDb.Name),
                        sql => sql.UseNetTopologySuite())
                    .AddInterceptors(
                        sp.GetRequiredService<AuditInterceptor>(),
                        sp.GetRequiredService<TenantInterceptor>(),
                        sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                    .UseSeedingSupport(sp), ServiceLifetime.Scoped);

            services.AddDbContextFactory<ConcertPrivilegedDbContext>((sp, opts) =>
                opts.UseSqlServer(
                        configuration.GetConnectionString(B2BDb.Name),
                        sql => sql.UseNetTopologySuite())
                    .AddInterceptors(
                        sp.GetRequiredService<AuditInterceptor>(),
                        sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                    .UseSeedingSupport(sp), ServiceLifetime.Scoped);

            services.AddDbContext<ConcertReadDbContext>((sp, opts) =>
                opts.UseSqlServer(
                        configuration.GetConnectionString(B2BDb.Name),
                        sql => sql.UseNetTopologySuite())
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));
            services.AddScoped<IConcertReadDbContext>(sp => sp.GetRequiredService<ConcertReadDbContext>());

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
            services.AddScoped<IPrivilegedUnitOfWorkBehavior, PrivilegedUnitOfWorkBehavior>();
            services.AddScoped<IOutboxUnitOfWorkBehavior, OutboxUnitOfWorkBehavior>();
            services.AddScoped<IPrivilegedOutboxUnitOfWorkBehavior, PrivilegedOutboxUnitOfWorkBehavior>();
            services.AddScoped<IConcertPrivilegedRepository, ConcertPrivilegedRepository>();
            services.AddScoped<IInvoicePrivilegedRepository, InvoicePrivilegedRepository>();
            services.AddScoped<IInvoiceSequenceRepository, InvoiceSequenceRepository>();

            // Services
            services.AddScoped<IConcertService, ConcertService>();
            services.AddScoped<IConcertWorkflow, ConcertWorkflow>();
            services.AddScoped<ISettlementService, SettlementService>();
            services.AddScoped<IConcertNotifier, ConcertNotifier>();
            services.AddScoped<IBookingConfirmationEmailSender, BookingConfirmationEmailSender>();
            services.AddScoped<IConcertDashboardService, ConcertDashboardService>();

            services.Configure<LegalSettings>(configuration.GetSection(LegalSettings.SectionName));
            services.AddScoped<IPdfBlobCache, PdfBlobCache>();
            services.AddScoped<InvoiceIssuer>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<IInvoicePdfRenderer, InvoicePdfRenderer>();
            services.AddScoped<ISelfBillingAgreementService, SelfBillingAgreementService>();
            services.AddClientContext();
            services.AddConcertDealStrategies();

            // Business-rule validators (interfaces in Concert.Application, impls in Concert.Infrastructure.Validators)
            services.AddSingleton<IConcertValidator, ConcertValidator>();
            services.AddScoped<IConcertAvailability, ConcertAvailability>();

            services.TryAddSingleton(typeof(IScoped<>), typeof(Scoped<>));
            services.AddScoped<ICompletionRunner, CompletionRunner>();

            // Repositories
            services.AddScoped<IConcertRepository, ConcertRepository>();
            services.AddScoped<IConcertCommandReceiptRepository, ConcertCommandReceiptRepository>();
            services.AddScoped<IConcertReadRepository, ConcertReadRepository>();
            services.AddScoped<IArtistReadModelRepository, ArtistReadModelRepository>();
            services.AddScoped<IVenueReadModelRepository, VenueReadModelRepository>();
            services.AddScoped<IConcertDashboardRepository, ConcertDashboardRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<ISelfBillingAgreementRepository, SelfBillingAgreementRepository>();

            // Query specifications
            services.AddScoped<IEndedSpecification, EndedSpecification>();
            services.AddScoped<IDoorRevenueOutstandingSpecification, DoorRevenueOutstandingSpecification>();

            // Mappers
            // Module facades
            services.AddScoped<IConcertModule, ConcertModule>();

            // Domain event -> integration event + read-model projection handlers
            services.AddScoped<IDomainEventHandler<ConcertChangedDomainEvent>, ConcertChangedDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<ConcertPostedDomainEvent>, ConcertPostedDomainEventHandler>();
            services.AddScoped<IDomainEventHandler<ConcertCancelledDomainEvent>, ConcertCancelledDomainEventHandler>();
            services.AddScoped<IIntegrationEventHandler<BookingConfirmedEvent>, BookingConfirmedIntegrationEventHandler>();
            services.AddScoped<IIntegrationEventHandler<ArtistChangedEvent>, ArtistReadModelProjectionHandler>();
            services.AddScoped<IIntegrationEventHandler<VenueChangedEvent>, VenueReadModelProjectionHandler>();
            services.AddScoped<IIntegrationEventHandler<CustomerReviewSubmittedEvent>, ConcertReviewProjectionHandler>();
            services.AddScoped<IIntegrationCommandHandler<NotifyConcertDraftCreatedCommand>,
                NotifyConcertDraftCreatedCommandHandler>();
            services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, SettlementPaymentProcessor>();
            services.AddScoped<IIntegrationEventHandler<TicketPurchasedEvent>, TicketSaleProcessor>();
            services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, SettlementPaymentFailedProcessor>();
            services.AddScoped<FinancialOperationOutcomeProcessor>();
            services.AddScoped<IIntegrationEventHandler<RefundEscrowSucceededEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
            services.AddScoped<IIntegrationEventHandler<RefundEscrowDeferredEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
            services.AddScoped<IIntegrationEventHandler<RefundEscrowRejectedEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());

            services.AddSingleton<ConcertConfigurationProvider>();
            services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ConcertConfigurationProvider>());

            return services;
        }

        internal IServiceCollection AddConcertDealStrategies()
        {
            services.AddScoped<ISettlementAmountResolver, SettlementAmountResolver>();

            return services.AddConcertDealStrategies(builder =>
            {
                builder.For(DealType.FlatFee)
                    .AddSingleton<ISettlementAmountResolver, FlatFeeSettlementAmount>()
                    .AddScoped<ICompleteStep, ReleaseEscrowCompleteStep>()
                    .AddScoped<ICancelStep, RefundEscrowCancelStep>();

                builder.For(DealType.DoorSplit)
                    .AddScoped<ISettlementAmountResolver, DoorSplitSettlementAmount>()
                    .AddScoped<ICompleteStep, PayoutCompleteStep>()
                    .AddScoped<ICancelStep, ImmediateCancelStep>();

                builder.For(DealType.Versus)
                    .AddScoped<ISettlementAmountResolver, VersusSettlementAmount>()
                    .AddScoped<ICompleteStep, PayoutCompleteStep>()
                    .AddScoped<ICancelStep, ImmediateCancelStep>();

                builder.For(DealType.VenueHire)
                    .AddSingleton<ISettlementAmountResolver, VenueHireSettlementAmount>()
                    .AddScoped<ICompleteStep, ReleaseEscrowCompleteStep>()
                    .AddScoped<ICancelStep, RefundEscrowCancelStep>();
            });
        }

        internal IServiceCollection AddConcertDealStrategies(
            Action<DealStrategyBuilder> configure)
        {
            var builder = new DealStrategyBuilder(services);
            configure(builder);
            builder.Build();

            return services;
        }

        public IServiceCollection AddConcertDevSeeder()
        {
            services.AddScoped<IDevSeeder, ConcertDevSeeder>();
            return services;
        }

        public IServiceCollection AddConcertTestSeeder()
        {
            services.AddScoped<ITestSeeder, ConcertTestSeeder>();
            return services;
        }
    }
}
