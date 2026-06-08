using Concertable.Seed.Shared;
using Concertable.Seed.Shared.Extensions;
using Concertable.B2B.Artist.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Validators;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
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
using Concertable.B2B.Concert.Infrastructure.Repositories;
using Concertable.B2B.Concert.Infrastructure.Services;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Concertable.B2B.Concert.Infrastructure.Services.Completion;
using Concertable.B2B.Concert.Infrastructure.Services.Payment;
using Concertable.B2B.Concert.Infrastructure.Validators;
using Concertable.B2B.Venue.Contracts.Events;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Concertable.DataAccess.Application;
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
                    configuration.GetConnectionString("B2BDb"),
                    sql => sql.UseNetTopologySuite())
                .AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>(),
                    sp.GetRequiredService<IDomainEventDispatchInterceptor>())
                .UseSeedingSupport(sp));

        services.AddScoped<IUnitOfWork<ConcertDbContext>, UnitOfWork<ConcertDbContext>>();
        services.AddScoped<IUnitOfWorkBehavior, UnitOfWorkBehavior>();

        // Services
        services.AddScoped<IConcertService, ConcertService>();
        services.AddScoped<IConcertDraftService, ConcertDraftService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IConcertNotifier, ConcertNotifier>();
        services.AddScoped<IOpportunityService, OpportunityService>();
        services.AddScoped<IOpportunitySyncer>(sp => new Sync.OpportunitySyncer(
            sp.GetRequiredService<IOpportunityRepository>(),
            sp.GetRequiredService<IContractModule>()));
        services.AddScoped<IApplicationService, ApplicationService>();

        services.AddScoped<ContractAccessor>();
        services.AddScoped<IContractAccessor>(sp => sp.GetRequiredService<ContractAccessor>());
        services.AddScoped<IContractResolver>(sp => sp.GetRequiredService<ContractAccessor>());
        services.AddScoped<IPayerLookup, PayerLookup>();

        // Business-rule validators (interfaces in Concert.Application, impls in Concert.Infrastructure.Validators)
        services.AddSingleton<IConcertValidator, ConcertValidator>();
        services.AddScoped<IApplicationValidator, ApplicationValidator>();

        services.AddScoped<IConcertCompletionRunner, ConcertCompletionRunner>();

        services.AddScoped<ILifecycleTransitioner, LifecycleTransitioner>();
        services.AddScoped<IConcertWorkflowFactory, ConcertWorkflowFactory>();

        services.AddScoped<IApplyExecutor, ApplyExecutor>();
        services.AddScoped<IAcceptExecutor, AcceptExecutor>();
        services.AddScoped<IVerifyExecutor, VerifyExecutor>();
        services.AddScoped<IEscrowExecutor, EscrowExecutor>();
        services.AddScoped<ISettlementExecutor, SettlementExecutor>();
        services.AddScoped<IFinishExecutor, FinishExecutor>();

        services.AddScoped<IApplyDispatcher, ApplyDispatcher>();
        services.AddScoped<IAcceptanceDispatcher, AcceptanceDispatcher>();
        services.AddScoped<ICheckoutDispatcher, CheckoutDispatcher>();
        services.AddScoped<IVerifyDispatcher, VerifyDispatcher>();
        services.AddScoped<IEscrowDispatcher, EscrowDispatcher>();
        services.AddScoped<ISettlementDispatcher, SettlementDispatcher>();
        services.AddScoped<ICompletionDispatcher, CompletionDispatcher>();

        services.AddConcertWorkflows();

        // Repositories
        services.AddScoped<IConcertRepository, ConcertRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IConcertDashboardRepository, ConcertDashboardRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Mappers
        services.AddScoped<IOpportunityMapper, OpportunityMapper>();
        services.AddScoped<IApplicationMapper, ApplicationMapper>();

        services.AddSingleton<IPaymentAmountMapper, PaymentAmountMapper>();
        services.AddSingleton<FlatFeePaymentAmountMapper>();
        services.AddSingleton<DoorSplitPaymentAmountMapper>();
        services.AddSingleton<VersusPaymentAmountMapper>();
        services.AddSingleton<VenueHirePaymentAmountMapper>();

        services.AddSingleton<IArtistShareCalculator, ArtistShareCalculator>();
        services.AddSingleton<DoorSplitCalculator>();
        services.AddSingleton<VersusCalculator>();

        // Module facades
        services.AddScoped<IConcertModule, ConcertModule>();
        services.AddScoped<IConcertWorkflowModule, ConcertWorkflowModule>();

        // Domain event -> integration event + read-model projection handlers
        services.AddScoped<IDomainEventHandler<ConcertChangedDomainEvent>, ConcertChangedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<ConcertPostedDomainEvent>, ConcertPostedDomainEventHandler>();
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

        services.AddSingleton<ConcertConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ConcertConfigurationProvider>());

        services.AddValidatorsFromAssemblyContaining<OpportunityDtoValidator>();

        return services;
    }

    public static IServiceCollection AddConcertWorkflows(this IServiceCollection services)
    {
        var catalog = new ConcertWorkflowCatalog();

        services.AddConcertWorkflow(catalog, ContractType.FlatFee, p => p
            .WithApply<SimpleApplyStep>()
            .WithCheckout<HoldCheckoutStep>()
            .WithAccept<CaptureEscrowAcceptStep>()
            .WithEscrowPayment()
            .WithBook<CreateConcertDraftStep>()
            .WithFinish<ReleaseEscrowFinishStep>(Complete)
            .WithWorkflow<FlatFeeWorkflow>());

        services.AddConcertWorkflow(catalog, ContractType.DoorSplit, p => p
            .WithApply<SimpleApplyStep>()
            .WithCheckout<VerifyCheckoutStep>()
            .WithAccept<PaidAcceptStep>()
            .WithVerifiedPayment()
            .WithBook<CreateConcertDraftStep>()
            .WithFinish<PayoutFinishStep>(AwaitingSettlement)
            .WithSettlement()
            .WithWorkflow<DoorSplitWorkflow>());

        services.AddConcertWorkflow(catalog, ContractType.Versus, p => p
            .WithApply<SimpleApplyStep>()
            .WithCheckout<VerifyCheckoutStep>()
            .WithAccept<PaidAcceptStep>()
            .WithVerifiedPayment()
            .WithBook<CreateConcertDraftStep>()
            .WithFinish<PayoutFinishStep>(AwaitingSettlement)
            .WithSettlement()
            .WithWorkflow<VersusWorkflow>());

        services.AddConcertWorkflow(catalog, ContractType.VenueHire, p => p
            .WithCheckout<SetupCheckoutStep>()
            .WithApply<PaidApplyStep>()
            .WithAccept<DepositEscrowAcceptStep>()
            .WithEscrowPayment()
            .WithBook<CreateConcertDraftStep>()
            .WithFinish<ReleaseEscrowFinishStep>(Complete)
            .WithWorkflow<VenueHireWorkflow>());

        services.AddSingleton<IConcertWorkflowCapabilityRegistry>(new ConcertWorkflowCapabilityRegistry(catalog.WorkflowTypes));
        services.AddSingleton<IConcertStateMachineRegistry>(new ConcertStateMachineRegistry(catalog.StateMachines));

        return services;
    }

    private static void AddConcertWorkflow(
        this IServiceCollection services,
        ConcertWorkflowCatalog catalog,
        ContractType contractType,
        Action<ConcertWorkflowBuilder> configure)
    {
        var builder = new ConcertWorkflowBuilder(contractType, services, catalog);
        configure(builder);
        builder.Build();
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
