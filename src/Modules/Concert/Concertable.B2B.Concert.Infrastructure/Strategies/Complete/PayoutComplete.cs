using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Application.Strategies;
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

        var result = await paymentsClient.PayVerifiedAsync(
            settlement.OperationId,
            settlement.PayerTenantId,
            settlement.PayeeTenantId,
            settlement.Gross,
            settlement.ApplicationId,
            settlement.BookingId,
            ct);
        if (result.TryGetError(out var error))
            return new FinishConcertError.ManagerPaymentFailure(error);
        if (!result.TryGetValue(out var outcome) || string.IsNullOrWhiteSpace(outcome.TransactionId))
            throw new InvalidOperationException(
                $"Settlement for concert {settlement.ConcertId} returned no transaction ID.");

        return new SettlementConfirmation.ManagerPaid(outcome.TransactionId);
    }
}
