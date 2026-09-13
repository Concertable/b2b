using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Concert.Infrastructure.Extensions;
using Concertable.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Settlement;

internal sealed class SettlementService : ISettlementService
{
    private readonly IUnitOfWorkBoundary unitOfWorkBoundary;
    private readonly InvoiceIssuer invoiceIssuer;
    private readonly ITenantModule tenantModule;
    private readonly ISelfBillingAgreementRepository selfBillingAgreementRepository;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SettlementService> logger;

    public SettlementService(
        IUnitOfWorkBoundary unitOfWorkBoundary,
        InvoiceIssuer invoiceIssuer,
        ITenantModule tenantModule,
        ISelfBillingAgreementRepository selfBillingAgreementRepository,
        TimeProvider timeProvider,
        ILogger<SettlementService> logger)
    {
        this.unitOfWorkBoundary = unitOfWorkBoundary;
        this.invoiceIssuer = invoiceIssuer;
        this.tenantModule = tenantModule;
        this.selfBillingAgreementRepository = selfBillingAgreementRepository;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result<SettlementPreparation, FinishConcertError>> ReserveAsync(
        int concertId,
        CancellationToken ct = default)
    {
        return await unitOfWorkBoundary.TryExecuteAsync(
            context => ReserveAsync(context, concertId, ct),
            exception => exception.IsConcertConcurrencyConflict(concertId),
            _ => ClassifyReservationConflictAsync(concertId, ct),
            ct);
    }

    public async Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        Guid operationId,
        CancellationToken ct = default) =>
        await unitOfWorkBoundary.ExecuteAsync(
            context => CompleteAsync(context, concertId, operationId, ct),
            ct);

    public async Task RecordFailureAsync(
        int concertId,
        Guid operationId,
        string code,
        string message,
        CancellationToken ct = default) =>
        await unitOfWorkBoundary.ExecuteAsync(
            context => RecordFailureAsync(context, concertId, operationId, code, message, ct),
            ct);

    // Re-runs the reservation against committed truth: whatever won the race decides the outcome, so a
    // concert cancelled underneath us reports its rejected transition rather than a lost update. The retry
    // is bounded — a second loss reports the state it lost to rather than escaping as an unclassified fault.
    private Task<Result<SettlementPreparation, FinishConcertError>> ClassifyReservationConflictAsync(
        int concertId,
        CancellationToken ct) =>
        unitOfWorkBoundary.TryExecuteAsync(
            context => ReserveAsync(context, concertId, ct),
            exception => exception.IsConcertConcurrencyConflict(concertId),
            _ => ReportContendedAsync(concertId, ct),
            ct);

    private Task<Result<SettlementPreparation, FinishConcertError>> ReportContendedAsync(
        int concertId,
        CancellationToken ct) =>
        unitOfWorkBoundary.ExecuteAsync(
            async context =>
            {
                var state = await context.Concerts
                    .Where(concert => concert.Id == concertId)
                    .Select(concert => (ConcertState?)concert.State)
                    .FirstOrDefaultAsync(ct);

                return state is { } current
                    ? (Result<SettlementPreparation, FinishConcertError>)new FinishConcertError.InvalidTransition(
                        new TransitionError<ConcertState, ConcertTrigger>(current, ConcertTrigger.BeginSettlement))
                    : new FinishConcertError.ConcertNotFound(concertId);
            },
            ct);

    private async Task<Result<SettlementPreparation, FinishConcertError>> ReserveAsync(
        ConcertDbContext context,
        int concertId,
        CancellationToken ct)
    {
        var concert = await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
        if (concert is null)
            return new FinishConcertError.ConcertNotFound(concertId);

        if (concert.State is ConcertState.Complete)
            return new SettlementPreparation.Terminal(SettlementOutcome.Settled);

        if (concert.State is ConcertState.AwaitingSettlement)
        {
            var prepared = CreatePreparation(
                concert,
                concert.SettlementOperationId
                ?? throw new InvalidOperationException(
                    $"Concert {concertId} awaits settlement without an operation."));
            return prepared;
        }

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (nowUtc < concert.Period.End)
            return new FinishConcertError.ConcertNotEnded();
        if (concert is DoorRevenueConcert { DoorRevenue: null })
            return new FinishConcertError.DoorRevenueRequired();

        var supplierTenantId = concert.SettlementPayeeTenantId;
        var customerTenantId = concert.SettlementPayerTenantId;
        var supplierComplete = await tenantModule.IsTaxComplianceCompleteAsync(supplierTenantId);
        var customerComplete = await tenantModule.IsTaxComplianceCompleteAsync(customerTenantId);
        if (!supplierComplete || !customerComplete)
        {
            logger.SettlementDeferredPendingTaxCompliance(
                concertId,
                supplierComplete ? customerTenantId : supplierTenantId);
            return new SettlementPreparation.Terminal(
                SettlementOutcome.DeferredPendingTaxCompliance);
        }

        if (!await selfBillingAgreementRepository.ExistsCurrentByTenantIdAsync(
                supplierTenantId,
                nowUtc,
                ct))
        {
            logger.SettlementDeferredPendingSelfBillingAgreement(concertId, supplierTenantId);
            return new SettlementPreparation.Terminal(
                SettlementOutcome.DeferredPendingSelfBillingAgreement);
        }

        var reservation = concert.BeginSettlement();
        if (reservation.TryGetError(out var transitionError))
            return new FinishConcertError.InvalidTransition(transitionError);
        if (!reservation.TryGetValue(out var operationId))
            throw new InvalidOperationException(
                $"Concert {concertId} settlement reservation returned no operation ID.");
        return CreatePreparation(concert, operationId);
    }

    private async Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        ConcertDbContext context,
        int concertId,
        Guid operationId,
        CancellationToken ct)
    {
        var concert = await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
        if (concert is null)
            return new FinishConcertError.ConcertNotFound(concertId);

        concert.EnsureSettlementOperation(operationId);
        if (concert.State is not ConcertState.Complete
            && concert.CompleteSettlement().TryGetError(out var transitionError))
            return new FinishConcertError.InvalidTransition(transitionError);

        await invoiceIssuer.IssueAsync(context, concert, ct);
        return SettlementOutcome.Settled;
    }

    private async Task RecordFailureAsync(
        ConcertDbContext context,
        int concertId,
        Guid operationId,
        string code,
        string message,
        CancellationToken ct)
    {
        var concert = await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct)
            ?? throw new InvalidOperationException($"Settlement concert {concertId} was not found.");
        concert.EnsureSettlementOperation(operationId);

        if (concert.State is ConcertState.Complete or ConcertState.SettlementFailed)
            return;

        var failure = concert.RecordSettlementFailure(code, message);
        if (failure.TryGetError(out var transitionError))
            throw new InvalidOperationException(
                $"Concert {concertId} cannot record settlement failure from {transitionError.Current}.");
    }

    private static SettlementPreparation.Ready CreatePreparation(
        ConcertEntity concert,
        Guid operationId) =>
        new(
            operationId,
            concert.Id,
            concert.DealType,
            concert.BookingId,
            concert.SettlementPaymentReference,
            concert.SettlementPayerTenantId,
            concert.SettlementPayeeTenantId,
            concert.SettlementGross);
}
