using Concertable.B2B.Booking.Contracts;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Data;
using Concertable.B2B.Tenant.Contracts;
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
        return await unitOfWorkBoundary.ExecuteAsync(
            context => ReserveAsync(context, concertId, ct),
            ct);
    }

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
        if (concert.RequiresDoorRevenue && concert.DoorRevenue is null)
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

    public async Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        int concertId,
        Guid operationId,
        SettlementConfirmation confirmation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(confirmation);
        return await unitOfWorkBoundary.ExecuteAsync(
            context => CompleteAsync(context, concertId, operationId, confirmation, ct),
            ct);
    }

    private async Task<Result<SettlementOutcome, FinishConcertError>> CompleteAsync(
        ConcertDbContext context,
        int concertId,
        Guid operationId,
        SettlementConfirmation confirmation,
        CancellationToken ct)
    {
        var concert = await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct);
        if (concert is null)
            return new FinishConcertError.ConcertNotFound(concertId);

        concert.EnsureSettlementOperation(operationId);
        EnsureConfirmationMatches(concert, confirmation);

        if (concert.State is ConcertState.Complete)
        {
            if (confirmation is SettlementConfirmation.ManagerPaid paid)
                concert.EnsureSettlementReference(paid.TransactionId);
        }
        else
        {
            var completion = confirmation switch
            {
                SettlementConfirmation.EscrowReleased => concert.CompleteSettlement(),
                SettlementConfirmation.ManagerPaid(var transactionId) =>
                    concert.CompleteSettlement(transactionId),
                _ => throw new InvalidOperationException(
                    $"Concert {concertId} received an unknown settlement confirmation.")
            };
            if (completion.TryGetError(out var transitionError))
                return new FinishConcertError.InvalidTransition(transitionError);
        }

        await invoiceIssuer.IssueAsync(context, concert, ct);
        return SettlementOutcome.Settled;
    }

    public async Task RecordFailureAsync(
        int concertId,
        Guid operationId,
        string providerReferenceId,
        string code,
        string message,
        CancellationToken ct = default)
    {
        await unitOfWorkBoundary.ExecuteAsync(
            context => RecordFailureAsync(
                context,
                concertId,
                operationId,
                providerReferenceId,
                code,
                message,
                ct),
            ct);
    }

    private async Task RecordFailureAsync(
        ConcertDbContext context,
        int concertId,
        Guid operationId,
        string providerReferenceId,
        string code,
        string message,
        CancellationToken ct)
    {
        var concert = await context.Concerts.SingleOrDefaultAsync(concert => concert.Id == concertId, ct)
            ?? throw new InvalidOperationException($"Settlement concert {concertId} was not found.");
        concert.EnsureSettlementOperation(operationId);

        if (concert.State is ConcertState.Complete)
        {
            return;
        }

        if (concert.State is ConcertState.SettlementFailed)
        {
            concert.EnsureSettlementReference(providerReferenceId);
            return;
        }

        var failure = concert.RecordSettlementFailure(providerReferenceId, code, message);
        if (failure.TryGetError(out var transitionError))
            throw new InvalidOperationException(
                $"Concert {concertId} cannot record settlement failure from {transitionError.Current}.");
    }

    private static void EnsureConfirmationMatches(
        ConcertEntity concert,
        SettlementConfirmation confirmation)
    {
        if ((concert.DealType is DealType.FlatFee or DealType.VenueHire &&
             confirmation is SettlementConfirmation.EscrowReleased) ||
            (concert.DealType is DealType.DoorSplit or DealType.Versus &&
             confirmation is SettlementConfirmation.ManagerPaid))
            return;

        throw new InvalidOperationException(
            $"Concert {concert.Id} cannot apply {confirmation.GetType().Name} to {concert.DealType} settlement.");
    }

    private static SettlementPreparation.Ready CreatePreparation(
        ConcertEntity concert,
        Guid operationId) =>
        new(
            operationId,
            concert.Id,
            concert.DealType,
            concert.BookingId,
            concert.SettlementPayerTenantId,
            concert.SettlementPayeeTenantId,
            concert.SettlementGross,
            concert.SettlementPaymentMethodId);
}
