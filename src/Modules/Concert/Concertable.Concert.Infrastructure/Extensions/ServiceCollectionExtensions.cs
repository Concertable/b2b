using Concertable.DataAccess;
using Concertable.Seeding;
using Concertable.Artist.Contracts.Events;
using Concertable.Concert.Application.Mappers;
using Concertable.Concert.Application.Validators;
using Concertable.Concert.Application.Workflow;
using Concertable.Concert.Application.Workflow.Executors;
using Concertable.Concert.Infrastructure.Services.Workflow.Dispatchers;
using Concertable.Concert.Infrastructure.Services.Workflow.Executors;
using Concertable.Concert.Infrastructure.Services.Workflow.Steps;
using Concertable.Concert.Infrastructure.Services.Workflow.Workflows;
using Concertable.Concert.Contracts;
using Concertable.Concert.Contracts.Events;
using Concertable.Concert.Domain.Events;
using Concertable.Concert.Infrastructure.Data;
using Concertable.Concert.Infrastructure.Data.Seeders;
using Concertable.Concert.Infrastructure.Events;
using Concertable.Concert.Infrastructure.Handlers;
using Concertable.Concert.Infrastructure.Repositories;
using Concertable.Concert.Infrastructure.Services;
using Concertable.Concert.Infrastructure.Services.Workflow;
using Concertable.Concert.Infrastructure.Services.Completion;
using Concertable.Concert.Infrastructure.Services.Payment;
using Concertable.Concert.Infrastructure.Validators;
using Concertable.DataAccess.Infrastructure;
using Concertable.Shared;
using Concertable.Venue.Contracts.Events;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.Concert.Infrastructure.Extensions;

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
                    sp.GetRequiredService<DomainEventDispatchInterceptor>()));

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

        services.AddScoped<IContractLoader, ContractLoader>();
        services.AddScoped<IPayerLookup, PayerLookup>();

        // Business-rule validators (interfaces in Concert.Application, impls in Concert.Infrastructure.Validators)
        services.AddSingleton<IConcertValidator, ConcertValidator>();
        services.AddScoped<IApplicationValidator, ApplicationValidator>();

        services.AddScoped<IConcertCompletionRunner, ConcertCompletionRunner>();

        services.AddScoped<IConcertTransitionValidatorFactory, ConcertTransitionValidatorFactory>();
        services.AddScoped<IConcertWorkflowFactory, ConcertWorkflowFactory>();
        services.AddScoped(typeof(ILifecycleRepository<>), typeof(LifecycleRepository<>));
        services.AddScoped(typeof(IWorkflowStateMachine<>), typeof(WorkflowStateMachine<>));

        services.AddScoped<IApplyExecutor, ApplyExecutor>();
        services.AddScoped<IAcceptExecutor, AcceptExecutor>();
        services.AddScoped<IVerifyExecutor, VerifyExecutor>();
        services.AddScoped<ISettleExecutor, SettleExecutor>();
        services.AddScoped<IFinishExecutor, FinishExecutor>();

        services.AddScoped<IApplyDispatcher, ApplyDispatcher>();
        services.AddScoped<IAcceptanceDispatcher, AcceptanceDispatcher>();
        services.AddScoped<ICheckoutDispatcher, CheckoutDispatcher>();
        services.AddScoped<IVerifyDispatcher, VerifyDispatcher>();
        services.AddScoped<ISettlementDispatcher, SettlementDispatcher>();
        services.AddScoped<ICompletionDispatcher, CompletionDispatcher>();

        var workflowTypes = new Dictionary<ContractType, Type>();

        workflowTypes[ContractType.FlatFee] = services.AddConcertWorkflow(ContractType.FlatFee, p => p
            .WithApply<SimpleApplyStep>()
            .WithCheckout<FlatFeeAcceptCheckoutStep>()
            .WithAccept<FlatFeeAcceptStep>()
            .WithSettle<NoOpSettleStep>()
            .WithFinish<FlatFeeFinishStep>()
            .WithWorkflow<FlatFeeWorkflow>());

        workflowTypes[ContractType.DoorSplit] = services.AddConcertWorkflow(ContractType.DoorSplit, p => p
            .WithApply<SimpleApplyStep>()
            .WithCheckout<DoorSplitAcceptCheckoutStep>()
            .WithVerify<DeferredVerifyStep>()
            .WithAccept<PaidAcceptStep>()
            .WithSettle<DeferredSettleStep>()
            .WithFinish<DoorSplitFinishStep>()
            .WithWorkflow<DoorSplitWorkflow>());

        workflowTypes[ContractType.Versus] = services.AddConcertWorkflow(ContractType.Versus, p => p
            .WithApply<SimpleApplyStep>()
            .WithCheckout<VersusAcceptCheckoutStep>()
            .WithVerify<DeferredVerifyStep>()
            .WithAccept<PaidAcceptStep>()
            .WithSettle<DeferredSettleStep>()
            .WithFinish<VersusFinishStep>()
            .WithWorkflow<VersusWorkflow>());

        workflowTypes[ContractType.VenueHire] = services.AddConcertWorkflow(ContractType.VenueHire, p => p
            .WithCheckout<VenueHireApplyCheckoutStep>()
            .WithApply<PaidApplyStep>()
            .WithAccept<VenueHireAcceptStep>()
            .WithSettle<NoOpSettleStep>()
            .WithFinish<VenueHireFinishStep>()
            .WithWorkflow<VenueHireWorkflow>());

        services.AddSingleton<IConcertWorkflowCapabilityRegistry>(new ConcertWorkflowCapabilityRegistry(workflowTypes));

        // Repositories
        services.AddScoped<IConcertRepository, ConcertRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<IApplicationRepository, ApplicationRepository>();
        services.AddScoped<IConcertDashboardRepository, ConcertDashboardRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Mappers
        services.AddScoped<IOpportunityMapper, OpportunityMapper>();
        services.AddScoped<IApplicationMapper, ApplicationMapper>();

        // Module facades
        services.AddScoped<IConcertModule, ConcertModule>();
        services.AddScoped<IConcertWorkflowModule, ConcertWorkflowModule>();

        // Domain event -> integration event + read-model projection handlers
        services.AddScoped<IDomainEventHandler<ConcertChangedDomainEvent>, ConcertChangedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<ApplicationAcceptedDomainEvent>, ApplicationAcceptedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<BookingSettledDomainEvent>, BookingSettledDomainEventHandler>();
        services.AddScoped<IIntegrationEventHandler<ArtistChangedEvent>, ArtistReadModelProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<VenueChangedEvent>, VenueReadModelProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<ReviewSubmittedEvent>, ConcertReviewProjectionHandler>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, SettlementPaymentProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, EscrowPaymentProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentSucceededEvent>, VerifyPaymentProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, BookingPaymentFailedProcessor>();
        services.AddScoped<IIntegrationEventHandler<PaymentFailedEvent>, VerifyPaymentFailedProcessor>();

        services.AddSingleton<ConcertConfigurationProvider>();
        services.AddSingleton<IEntityTypeConfigurationProvider>(sp => sp.GetRequiredService<ConcertConfigurationProvider>());
        services.AddSingleton<IRatingProjectionConfigurationProvider, ConcertRatingProjectionConfigurationProvider>();

        services.AddValidatorsFromAssemblyContaining<OpportunityDtoValidator>();

        return services;
    }

    private static Type AddConcertWorkflow(
        this IServiceCollection services,
        ContractType contractType,
        Action<ConcertWorkflowBuilder> configure)
    {
        var builder = new ConcertWorkflowBuilder(contractType, services);
        configure(builder);
        return builder.Build();
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
