using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class FinishExecutor : IFinishExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IDealResolver dealResolver;
    private readonly IConcertRepository concertRepository;
    private readonly ISettlementPayeeResolver settlementPayeeResolver;
    private readonly ITicketPayeeResolver ticketPayeeResolver;
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
        ISettlementPayeeResolver settlementPayeeResolver,
        ITicketPayeeResolver ticketPayeeResolver,
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
        this.settlementPayeeResolver = settlementPayeeResolver;
        this.ticketPayeeResolver = ticketPayeeResolver;
        this.invoiceIssuer = invoiceIssuer;
        this.tenantModule = tenantModule;
        this.selfBillingAgreementGate = selfBillingAgreementGate;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<FluentResults.Result<SettlementOutcome>> FinishAsync(int concertId, CancellationToken ct = default)
    {
        try
        {
            var concert = await concertRepository.GetByIdWithBookingAsync(concertId, ct)
                .OrNotFound();
            if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
                throw new BadRequestException("Concert cannot be finished before it has ended");

            // Fail-closed tax gate: both parties' tax identities must be complete for their jurisdiction — the
            // payee's so we can settle, and the counterparty's so the self-billed invoice minted in the same
            // transaction carries both parties' legally-required VAT details. If either is incomplete, don't
            // transition, don't pay, don't invoice; the hourly sweep self-heals once the missing details land.
            var supplierTenantId = settlementPayeeResolver.ResolveTenantId(concert);
            var customerTenantId = ticketPayeeResolver.ResolveTenantId(concert);
            var supplierComplete = await tenantModule.IsTaxComplianceCompleteAsync(supplierTenantId);
            var customerComplete = await tenantModule.IsTaxComplianceCompleteAsync(customerTenantId);
            if (!supplierComplete || !customerComplete)
            {
                logger.SettlementDeferredPendingTaxCompliance(concertId, supplierComplete ? customerTenantId : supplierTenantId);
                return FluentResults.Result.Ok(SettlementOutcome.DeferredPendingTaxCompliance);
            }

            // Fail-closed self-billing gate: the invoice minted below prints that it is raised by Concertable on the
            // supplier's behalf under a self-billing agreement, so that agreement must actually be in force. Without a
            // current one, defer rather than assert a document we do not hold; the sweep self-heals once consent lands.
            if (!await selfBillingAgreementGate.HasCurrentAsync(supplierTenantId, timeProvider.GetUtcNow().UtcDateTime, ct))
            {
                logger.SettlementDeferredPendingSelfBillingAgreement(concertId, supplierTenantId);
                return FluentResults.Result.Ok(SettlementOutcome.DeferredPendingSelfBillingAgreement);
            }

            await transitioner.TransitionAsync(concert.Booking.ApplicationId, Trigger.Finish, async app =>
            {
                await dealResolver.ResolveByConcertIdAsync(concertId);
                var workflow = workflows.Create(app.DealType);
                await workflow.Finish.ExecuteAsync(concertId);
                await invoiceIssuer.IssueAsync(concert);
            }, ct).GetValueOrThrowAsync();
            return FluentResults.Result.Ok(SettlementOutcome.Settled);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.FailedToFinishConcert(concertId, ex);
            return FluentResults.Result.Fail<SettlementOutcome>(ex.Message);
        }
    }
}
