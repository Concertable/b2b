using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Infrastructure.Payments;
using Concertable.Payment.Contracts.Errors;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;

internal sealed class PayoutCompleteStep : ICompleteStep
{
    private readonly ISettlementOperationsClient settlementOperationsClient;
    private readonly ILogger<PayoutCompleteStep> logger;

    public PayoutCompleteStep(
        ISettlementOperationsClient settlementOperationsClient,
        ILogger<PayoutCompleteStep> logger)
    {
        this.settlementOperationsClient = settlementOperationsClient;
        this.logger = logger;
    }

    public async Task<UnitResult<FinishConcertError>> CompleteAsync(
        SettlementPreparation.Ready settlement,
        CancellationToken ct = default)
    {
        logger.ArtistShareCalculated(settlement.ConcertId, settlement.Gross.Amount);
        logger.SettlingConcert(
            settlement.ConcertId,
            settlement.BookingId,
            settlement.Gross.Amount,
            settlement.PayerTenantId,
            settlement.PayeeTenantId);

        var result = await settlementOperationsClient.PayAsync(
            settlement.OperationId,
            PaymentOperationReferences.Settlement(settlement.ConcertId),
            settlement.PayerTenantId,
            settlement.PayeeTenantId,
            settlement.Gross,
            settlement.Commitment,
            PaymentSession.OffSession,
            ct);
        if (!result.TryGetError(out var error))
            return new Success();

        return error switch
        {
            PaymentMethodChargeError.PaymentMethodFailure(var methodError) =>
                new FinishConcertError.PaymentCommitmentFailure(methodError),
            PaymentMethodChargeError.AuthenticationRequired =>
                new FinishConcertError.PaymentAuthenticationRequired(),
            PaymentMethodChargeError.PaymentFailure(var paymentError) =>
                new FinishConcertError.SettlementChargeFailure(paymentError),
            PaymentMethodChargeError.CommissionFailure(var commissionError) =>
                new FinishConcertError.SettlementCommissionFailure(commissionError),
            PaymentMethodChargeError.OperationConflict =>
                new FinishConcertError.SettlementOperationConflict(),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
        };
    }
}
