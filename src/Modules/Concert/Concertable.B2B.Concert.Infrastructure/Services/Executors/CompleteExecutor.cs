using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.State;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Executors;

internal sealed class CompleteExecutor : ICompleteExecutor
{
    private readonly IConcertRepository concerts;
    private readonly IConcertDealStrategyFactory<ICompleteStep> steps;
    private readonly IInvoiceIssuer invoiceIssuer;
    private readonly ITenantModule tenants;
    private readonly ISelfBillingAgreementGate selfBillingAgreements;
    private readonly IUnitOfWorkBehavior unitOfWork;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<CompleteExecutor> logger;

    public CompleteExecutor(
        IConcertRepository concerts,
        IConcertDealStrategyFactory<ICompleteStep> steps,
        IInvoiceIssuer invoiceIssuer,
        ITenantModule tenants,
        ISelfBillingAgreementGate selfBillingAgreements,
        IUnitOfWorkBehavior unitOfWork,
        TimeProvider timeProvider,
        ILogger<CompleteExecutor> logger)
    {
        this.concerts = concerts;
        this.steps = steps;
        this.invoiceIssuer = invoiceIssuer;
        this.tenants = tenants;
        this.selfBillingAgreements = selfBillingAgreements;
        this.unitOfWork = unitOfWork;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var concert = await concerts.GetByIdForLifecycleAsync(concertId, ct);
            if (concert is null)
                return new FinishConcertError.ConcertNotFound(concertId);
            if (concert.State is ConcertState.Complete or ConcertState.AwaitingSettlement)
                return SettlementOutcome.Settled;
            if (concert.State is ConcertState.CancellationPending or ConcertState.CancellationFailed or ConcertState.Cancelled)
                return new FinishConcertError.InvalidState(concert.State);
            if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
                return new FinishConcertError.ConcertNotEnded();
            if (concert.RequiresDoorRevenue && concert.DoorRevenue is null)
                return new FinishConcertError.DoorRevenueRequired();

            var supplier = concert.SettlementPayeeTenantId;
            var customer = concert.SettlementPayerTenantId;
            var supplierComplete = await tenants.IsTaxComplianceCompleteAsync(supplier);
            var customerComplete = await tenants.IsTaxComplianceCompleteAsync(customer);
            if (!supplierComplete || !customerComplete)
            {
                logger.SettlementDeferredPendingTaxCompliance(
                    concertId,
                    supplierComplete ? customer : supplier);
                return SettlementOutcome.DeferredPendingTaxCompliance;
            }

            if (!await selfBillingAgreements.HasCurrentAsync(
                    supplier,
                    timeProvider.GetUtcNow().UtcDateTime,
                    ct))
            {
                logger.SettlementDeferredPendingSelfBillingAgreement(concertId, supplier);
                return SettlementOutcome.DeferredPendingSelfBillingAgreement;
            }

            var completion = await steps.Create(concert.DealType).ExecuteAsync(concert, ct);
            if (completion.TryGetError(out var error))
                return error;
            if (concert.State is ConcertState.Complete)
                await invoiceIssuer.IssueAsync(concert, ct);

            await concerts.SaveChangesAsync(ct);
            return SettlementOutcome.Settled;
        }, ct);
}
