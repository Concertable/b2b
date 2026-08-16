using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Renderers;
using Concertable.B2B.Concert.Application.Resolvers;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Application.Validators;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Dispatchers;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;
using Concertable.B2B.Concert.Contracts;
using Concertable.B2B.Concert.Contracts.Events;
using Concertable.B2B.Concert.Domain.Events;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Concert.Infrastructure.Data.Seeders;
using Concertable.B2B.Concert.Infrastructure.Events;
using Concertable.B2B.Concert.Infrastructure.Handlers;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.B2B.Concert.Infrastructure.Repositories;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Concert.Infrastructure.Services.Strategies;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Concertable.B2B.Concert.Infrastructure.Services.Settlement;
using Concertable.B2B.Concert.Infrastructure.Services.Completion;
using Concertable.B2B.Concert.Infrastructure.Services.Payment;
using Concertable.B2B.Concert.Infrastructure.Specifications;
using Concertable.B2B.Concert.Infrastructure.Validators;
using Concertable.B2B.Venue.Contracts.Events;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Data;
using Concertable.Messaging.Contracts;
using Concertable.Kernel;
using static Concertable.B2B.Concert.Domain.Lifecycle.LifecycleState;

namespace Concertable.B2B.Concert.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConcertModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ConcertDbContext>((sp, opts) =>
            opts.UseSqlServer(
                    configuration.GetConnectionString(B2BDb.Name),
                    sql => sql.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<TenantInterceptor>(),
                    sp.GetRequiredService<VenueArtistTenantInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

        services.AddDbContext<PublicConcertDbContext>((sp, opts) =>
            opts.UseSqlServer(
                    configuration.GetConnectionString(B2BDb.Name),
                    sql => sql.UseNetTopologySuite())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IUnitOfWork<ConcertDbContext>, UnitOfWork<ConcertDbContext>>();
        services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();
        services.AddScoped<IOutboxUnitOfWorkBehavior, OutboxUnitOfWorkBehavior>();

        // Services
        services.AddScoped<IConcertService, ConcertService>();
        services.AddScoped<IConcertDraftService, ConcertDraftService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IConcertNotifier, ConcertNotifier>();
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<IOpportunityDashboardService, OpportunityDashboardService>();
        services.AddScoped<IOpportunitySyncer>(sp => new Sync.OpportunitySyncer(
            sp.GetRequiredService<IOpportunityRepository>(),
            sp.GetRequiredService<IDealModule>()));
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IApplicationNotifier, ApplicationNotifier>();
        services.AddScoped<IMessenger, Messenger>();
        services.AddScoped<IConcertDashboardService, ConcertDashboardService>();

        services.Configure<LegalSettings>(configuration.GetSection(LegalSettings.SectionName));
        services.AddScoped<IPdfBlobCache, PdfBlobCache>();
        services.AddScoped<IContractIssuer, ContractIssuer>();
        services.AddScoped<IContractService, ContractService>();
        services.AddScoped<IContractPdfRenderer, ContractPdfRenderer>();
        services.AddScoped<IInvoiceIssuer, InvoiceIssuer>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IInvoicePdfRenderer, InvoicePdfRenderer>();
        services.AddScoped<ISelfBillingAgreementService, SelfBillingAgreementService>();
        services.AddScoped<ISelfBillingAgreementGate, SelfBillingAgreementGate>();
        services.AddScoped<IClientContext, ClientContextAccessor>();
        services.AddConcertDealStrategies();

        services.AddScoped<DealAccessor>();
        services.AddScoped<IDealAccessor>(sp => sp.GetRequiredService<DealAccessor>());
        services.AddScoped<IDealResolver>(sp => sp.GetRequiredService<DealAccessor>());

        // Business-rule validators (interfaces in Concert.Application, impls in Concert.Infrastructure.Validators)
        services.AddSingleton<IConcertValidator, ConcertValidator>();
        services.AddScoped<IApplicationValidator, ApplicationValidator>();
        services.AddScoped<IConcertAvailability, ConcertAvailability>();

        services.TryAddSingleton(typeof(IScoped<>), typeof(Scoped<>));
        services.AddScoped<IConcertCompletionRunner, ConcertCompletionRunner>();

        services.AddScoped<ILifecycleTransitioner, LifecycleTransitioner>();
        services.AddScoped<IConcertWorkflowFactory, ConcertWorkflowFactory>();

        services.AddScoped<IApplyExecutor, ApplyExecutor>();
        services.AddScoped<IAcceptExecutor, AcceptExecutor>();
        services.AddScoped<IVerifyExecutor, VerifyExecutor>();
        services.AddScoped<IVerifyCoordinator, VerifyCoordinator>();
        services.AddScoped<IBookingAdvancer, BookingAdvancer>();
        services.AddScoped<IPaymentVerificationRecorder, PaymentVerificationRecorder>();
        services.AddScoped<IEscrowExecutor, EscrowExecutor>();
        services.AddScoped<ISettlementExecutor, SettlementExecutor>();
        services.AddScoped<IFinishExecutor, FinishExecutor>();
        services.AddScoped<ICancelExecutor, CancelExecutor>();
        services.AddScoped<IWithdrawExecutor, WithdrawExecutor>();
        services.AddScoped<IRejectExecutor, RejectExecutor>();
        services.AddScoped<ICancelApplicationExecutor, CancelApplicationExecutor>();
        services.AddScoped<IApplicationExecutor, ApplicationExecutor>();
        services.AddScoped<IApplicationCancelStep, RefundEscrowByApplicationStep>();

        services.AddScoped<ICheckoutDispatcher, CheckoutDispatcher>();

        // Repositories
        services.AddScoped<IConcertRepository, ConcertRepository>();
        services.AddScoped<IPublicConcertRepository, PublicConcertRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<IPublicOpportunityRepository, PublicOpportunityRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IConcertDashboardRepository, ConcertDashboardRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IPublicBookingRepository, PublicBookingRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ISelfBillingAgreementRepository, SelfBillingAgreementRepository>();
        services.AddScoped(typeof(ISequenceRepository<>), typeof(SequenceRepository<>));

        // Query specifications
        services.AddScoped<IEndedAndBookedSpecification, EndedAndBookedSpecification>();
        services.AddScoped<IDoorRevenueOutstandingSpecification, DoorRevenueOutstandingSpecification>();

        // Mappers
        services.AddScoped<IOpportunityMapper, OpportunityMapper>();
        services.AddScoped<IApplicationMapper, ApplicationMapper>();

        // Module facades
        services.AddScoped<IConcertModule, ConcertModule>();

        // Domain event -> integration event + read-model projection handlers
        services.AddScoped<IDomainEventHandler<ConcertChangedDomainEvent>, ConcertChangedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<ConcertPostedDomainEvent>, ConcertPostedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<ConcertCancelledDomainEvent>, ConcertCancelledDomainEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ArtistChangedEvent>, ArtistReadModelProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<VenueChangedEvent>, VenueReadModelProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<CustomerReviewSubmittedEvent>, ConcertReviewProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, SettlementPaymentProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, EscrowPaymentProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, VerifyPaymentProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, TicketSaleProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, EscrowPaymentFailedProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, SettlementPaymentFailedProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, VerifyPaymentFailedProcessor>();
        services.AddScoped<FinancialOperationOutcomeProcessor>();
        services.AddScoped<IIntegrationEventHandler<CaptureEscrowSucceededEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
        services.AddScoped<IIntegrationEventHandler<CaptureEscrowRejectedEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
        services.AddScoped<IIntegrationEventHandler<DepositEscrowSucceededEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
        services.AddScoped<IIntegrationEventHandler<DepositEscrowRejectedEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
        services.AddScoped<IIntegrationEventHandler<RefundEscrowSucceededEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
        services.AddScoped<IIntegrationEventHandler<RefundEscrowRejectedEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());
        services.AddScoped<IIntegrationEventHandler<RefundEscrowDeferredEvent>>(sp => sp.GetRequiredService<FinancialOperationOutcomeProcessor>());

        services.AddSingleton<ConcertConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ConcertConfigurationProvider>());

        services.AddValidatorsFromAssemblyContaining<OpportunityDtoValidator>();

        return services;
    }

    internal static IServiceCollection AddConcertDealStrategies(this IServiceCollection services)
    {
        services.AddScoped<ITermsFingerprintCalculator, TermsFingerprintCalculator>();
        services.AddScoped<IDealTermsRenderer, DealTermsRenderer>();
        services.AddScoped<IDealTermsSerializer, DealTermsSerializer>();
        services.AddScoped<IDealPayeeResolver, DealPayeeResolver>();
        services.AddScoped<IPaymentAmountMapper, PaymentAmountMapper>();
        services.AddScoped<ISettlementAmountResolver, SettlementAmountResolver>();

        return services.AddConcertDealStrategies(strategies =>
        {
            strategies.For(DealType.FlatFee)
                .AddSingleton<IDealTerms, FlatFeeDealTerms>()
                .AddSingleton<IDealPayeeResolver, VenuePaysArtistDealPayeeResolver>()
                .AddSingleton<IPaymentAmountMapper, FlatFeePaymentAmountMapper>()
                .AddSingleton<ISettlementAmountResolver, FlatFeeSettlementAmount>()
                .AddWorkflow<FlatFeeWorkflow>(workflow => workflow
                    .WithApply<SimpleApplyStep>()
                    .WithCheckout<HoldCheckoutStep>()
                    .WithAccept<CaptureEscrowAcceptStep>()
                    .WithEscrowPayment()
                    .WithBook<CreateConcertDraftStep>()
                    .WithFinish<ReleaseEscrowFinishStep>(Complete)
                    .WithCancel<RefundEscrowStep>()
                    .WithApplicationCancel());

            strategies.For(DealType.DoorSplit)
                .AddSingleton<IDealTerms, DoorSplitDealTerms>()
                .AddSingleton<IDealPayeeResolver, VenuePaysArtistDealPayeeResolver>()
                .AddSingleton<IPaymentAmountMapper, DoorSplitPaymentAmountMapper>()
                .AddScoped<ISettlementAmountResolver, DoorSplitSettlementAmount>()
                .AddWorkflow<DoorSplitWorkflow>(workflow => workflow
                    .WithApply<SimpleApplyStep>()
                    .WithCheckout<VerifyCheckoutStep>()
                    .WithAccept<PaidAcceptStep>()
                    .WithVerifiedPayment()
                    .WithBook<CreateConcertDraftStep>()
                    .WithFinish<PayoutFinishStep>(AwaitingSettlement)
                    .WithSettlement()
                    .WithCancel<RefundEscrowStep>()
                    .WithApplicationCancel());

            strategies.For(DealType.Versus)
                .AddSingleton<IDealTerms, VersusDealTerms>()
                .AddSingleton<IDealPayeeResolver, VenuePaysArtistDealPayeeResolver>()
                .AddSingleton<IPaymentAmountMapper, VersusPaymentAmountMapper>()
                .AddScoped<ISettlementAmountResolver, VersusSettlementAmount>()
                .AddWorkflow<VersusWorkflow>(workflow => workflow
                    .WithApply<SimpleApplyStep>()
                    .WithCheckout<VerifyCheckoutStep>()
                    .WithAccept<PaidAcceptStep>()
                    .WithVerifiedPayment()
                    .WithBook<CreateConcertDraftStep>()
                    .WithFinish<PayoutFinishStep>(AwaitingSettlement)
                    .WithSettlement()
                    .WithCancel<RefundEscrowStep>()
                    .WithApplicationCancel());

            strategies.For(DealType.VenueHire)
                .AddSingleton<IDealTerms, VenueHireDealTerms>()
                .AddSingleton<IDealPayeeResolver, ArtistPaysVenueDealPayeeResolver>()
                .AddSingleton<IPaymentAmountMapper, VenueHirePaymentAmountMapper>()
                .AddSingleton<ISettlementAmountResolver, VenueHireSettlementAmount>()
                .AddWorkflow<VenueHireWorkflow>(workflow => workflow
                    .WithCheckout<SetupCheckoutStep>()
                    .WithApply<PaidApplyStep>()
                    .WithAccept<DepositEscrowAcceptStep>()
                    .WithEscrowPayment()
                    .WithBook<CreateConcertDraftStep>()
                    .WithFinish<ReleaseEscrowFinishStep>(Complete)
                    .WithCancel<RefundEscrowStep>()
                    .WithApplicationCancel());

            strategies.RequireAll<IDealTerms>();
            strategies.RequireAll<IDealPayeeResolver>();
            strategies.RequireAll<IPaymentAmountMapper>();
            strategies.RequireAll<ISettlementAmountResolver>();
            strategies.RequireAll<IConcertWorkflow>();
        });
    }

    internal static IServiceCollection AddConcertDealStrategies(
        this IServiceCollection services,
        Action<ConcertDealStrategyBuilder> configure)
    {
        var builder = new ConcertDealStrategyBuilder(services);
        configure(builder);
        builder.Build();

        services.TryAddScoped<IKeyedServiceProvider>(sp => (IKeyedServiceProvider)sp);
        services.TryAddScoped(typeof(IConcertDealStrategyFactory<>), typeof(ConcertDealStrategyFactory<>));
        return services;
    }

    public static IServiceCollection AddConcertDevSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDevSeeder, ConcertDevSeeder>();
        return services;
    }

    public static IServiceCollection AddConcertTestSeeder(this IServiceCollection services)
    {
        services.AddScoped<ITestSeeder, ConcertTestSeeder>();
        return services;
    }

}
