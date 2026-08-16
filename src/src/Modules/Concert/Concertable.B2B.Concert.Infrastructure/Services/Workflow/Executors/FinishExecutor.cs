using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class FinishExecutor : IFinishExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IDealResolver dealResolver;
    private readonly IConcertRepository concertRepository;
    private readonly IDealPayeeResolver dealPayeeResolver;
    private readonly IInvoiceIssuer invoiceIssuer;
    private readonly ITenantModule tenantModule;
    private readonly ISelfBillingAgreementGate selfBillingAgreementGate;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<FinishExecutor> logger;

    public FinishExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IDealResolver dealResolver,
        IConcertRepository concertRepository,
        IDealPayeeResolver dealPayeeResolver,
        IInvoiceIssuer invoiceIssuer,
        ITenantModule tenantModule,
        ISelfBillingAgreementGate selfBillingAgreementGate,
        TimeProvider timeProvider,
        ILogger<FinishExecutor> logger)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.dealResolver = dealResolver;
        this.concertRepository = concertRepository;
        this.dealPayeeResolver = dealPayeeResolver;
        this.invoiceIssuer = invoiceIssuer;
        this.tenantModule = tenantModule;
        this.selfBillingAgreementGate = selfBillingAgreementGate;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<SettlementOutcome, FinishConcertError>> FinishAsync(
        int concertId,
        CancellationToken ct = default)
    {
        var concert = await concertRepository.GetByIdWithBookingAsync(concertId, ct);
        if (concert is null)
            return new FinishConcertError.ConcertNotFound(concertId);

        if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
            return new FinishConcertError.ConcertNotEnded();

        var supplierTenantId = dealPayeeResolver.ResolveSettlementTenantId(concert);
        var customerTenantId = dealPayeeResolver.ResolveTicketTenantId(concert);
        var supplierComplete = await tenantModule.IsTaxComplianceCompleteAsync(supplierTenantId);
        var customerComplete = await tenantModule.IsTaxComplianceCompleteAsync(customerTenantId);
        if (!supplierComplete || !customerComplete)
        {
            logger.SettlementDeferredPendingTaxCompliance(concertId, supplierComplete ? customerTenantId : supplierTenantId);
            return SettlementOutcome.DeferredPendingTaxCompliance;
        }

        if (!await selfBillingAgreementGate.HasCurrentAsync(supplierTenantId, timeProvider.GetUtcNow().UtcDateTime, ct))
        {
            logger.SettlementDeferredPendingSelfBillingAgreement(concertId, supplierTenantId);
            return SettlementOutcome.DeferredPendingSelfBillingAgreement;
        }

        var transition = await transitioner.TransitionAsync<FinishConcertError>(
            concert.Booking.ApplicationId,
            Trigger.Finish,
            error => (FinishConcertError)new FinishConcertError.TransitionFailure(error),
            async app =>
            {
                await dealResolver.ResolveByConcertIdAsync(concertId);
                var workflow = workflows.Create(app.DealType);
                var finish = await workflow.Finish.ExecuteAsync(concertId, ct);
                if (finish.TryGetError(out var finishError))
                    return finishError;

                await invoiceIssuer.IssueAsync(concert);
                return new Success();
            }, ct);

        if (transition.TryGetError(out var transitionError))
            return transitionError;

        return SettlementOutcome.Settled;
    }
}
