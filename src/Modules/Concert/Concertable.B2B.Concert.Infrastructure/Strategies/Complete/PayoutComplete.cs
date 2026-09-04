using Concertable.B2B.Concert.Application.Models;
using Concertable.Payment.Contracts.Errors;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Infrastructure.Payments;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Strategies;


internal sealed class PayoutComplete : IComplete
{
    private readonly IManagerPaymentOperationsClient paymentsClient;
    private readonly ILogger<PayoutComplete> logger;

    public PayoutComplete(
        IManagerPaymentOperationsClient paymentsClient,
        ILogger<PayoutComplete> logger)
    {
        this.paymentsClient = paymentsClient;
        this.logger = logger;
    }

    public async Task<Result<SettlementConfirmation, FinishConcertError>> CompleteAsync(
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

        var result = await paymentsClient.PayAsync(
            settlement.OperationId,
            settlement.PayerTenantId,
            settlement.PayeeTenantId,
            settlement.Gross,
            settlement.Commitment.ToReference(),
            PaymentSession.OffSession,
            settlement.BookingId,
            ct);
        if (result.TryGetError(out var error))
            // The three arms recover differently: a bad commitment needs a new setup, an authentication
            // challenge needs the payer back on-session against this same reference, and a plain decline
            // is the charge failure it always was. Collapsing them would undo that distinction.
            return error switch
            {
                PaymentMethodChargeError.PaymentMethodFailure(var methodError) =>
                    new FinishConcertError.PaymentCommitmentFailure(methodError),
                PaymentMethodChargeError.AuthenticationRequired =>
                    new FinishConcertError.PaymentAuthenticationRequired(),
                PaymentMethodChargeError.ChargeFailure(var chargeError) =>
                    new FinishConcertError.ManagerPaymentFailure(chargeError),
                _ => throw new ArgumentOutOfRangeException(nameof(error), error, null)
            };
        if (!result.TryGetValue(out var outcome) || string.IsNullOrWhiteSpace(outcome.TransactionId))
            throw new InvalidOperationException(
                $"Settlement for concert {settlement.ConcertId} returned no transaction ID.");

        return new SettlementConfirmation.ManagerPaid(outcome.TransactionId);
    }
}
